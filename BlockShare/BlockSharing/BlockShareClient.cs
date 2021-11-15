using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands;
using BlockShare.BlockSharing.DirectoryDigesting;
using BlockShare.BlockSharing.HashLists;
using BlockShare.BlockSharing.NetworkStatistics;
using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    class BlockShareClient : IDisposable
    {
        private TcpClient tcpClient;

        private Stream networkStream;

        private List<X509Certificate> acceptableCertificates = new List<X509Certificate>();

        private Preferences preferences;

        private NetStat clientNetStat = new NetStat();
        private bool disposedValue;

        public NetStat CloneNetStat() => clientNetStat.CloneNetStat();
        public void ClearNetStat()
        {
            clientNetStat.Clear();
        }

        public ILogger Logger { get; set; }

        #region Events
        public event Action<BlockShareClient, string, double> OnHashingProgressChanged;
        public event Action<BlockShareClient, string> OnHashingFinished;
        public event Action<BlockShareClient, DownloadingProgressEventData> OnBlockDownloaded;
        public event Action<BlockShareClient, string> OnDownloadingFinished;
        #endregion

        private void Log(string message, int withVerbosity)
        {
            if (withVerbosity <= preferences.Verbosity)
            {
                Logger?.Log(message);
            }
        }

        private bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if(sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (certificate == null)
            {
                Log("No certificate was provided", 0);
                return false;
            }

            foreach (X509Certificate acceptedCert in acceptableCertificates)
            {
                if (Utils.CompareBytes(acceptedCert.GetCertHash(), certificate.GetCertHash()))
                {
                    return true;
                }
            }

            Log($"Server certificate validation error: {sslPolicyErrors}", 0);
            return false;
        }

        private X509Certificate SelectClientCertificate(
            object sender,
            string targetHost,
            X509CertificateCollection certificateCollection,
            X509Certificate remoteCertificate,
            string[] accepableIssuers)
        {
            return certificateCollection[0];
        }

        public BlockShareClient(Preferences preferences, ILogger logger)
        {
            this.preferences = preferences;
            Logger = logger;

            Log($"Connecting to server {preferences.ServerIp} {preferences.ServerPort}...", 0);

            tcpClient = new TcpClient();

            tcpClient.Connect(preferences.ServerIp, preferences.ServerPort);

            if(preferences.SecurityPreferences != null && preferences.SecurityPreferences.Method != SecurityMethod.None)
            {
                Log($"Using security method: {preferences.SecurityPreferences.Method}", 0);

                acceptableCertificates.AddRange(Utils.GetCertificates(preferences.SecurityPreferences.AcceptedCertificatesDirectoryPath));

                Log($"Accepted certificates count: {acceptableCertificates.Count}", 0);

                X509Certificate clientCertificate = null;
                clientCertificate = Utils.CreateFromPkcs12(preferences.SecurityPreferences.ClientCertificatePath);

                Log($"Client certificate: {clientCertificate.GetCertHashString()}", 0);

                X509CertificateCollection clientCertificates = new X509CertificateCollection() { clientCertificate };

                NetworkStream basicStream = tcpClient.GetStream();

                SslStream sslStream = new SslStream(
                    basicStream,
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    new LocalCertificateSelectionCallback(SelectClientCertificate),
                    EncryptionPolicy.RequireEncryption
                    );
                try
                {
                    sslStream.AuthenticateAsClient(preferences.SecurityPreferences.ServerName, clientCertificates, true);
                }
                catch(AuthenticationException e)
                {
                    Log($"Authentication Exception: {e.Message}", 0);
                    if (e.InnerException != null)
                    {
                        Log("Inner exception: {e.InnerException.Message}", 0);
                    }
                    Log("Authentication failed - closing the connection.", 0);
                    tcpClient.Close();

                    throw;
                }

                networkStream = sslStream;
            }
            else
            {
                Log("Using no security mechanisms", 0);
                networkStream = tcpClient.GetStream();
            }

            Log($"Connected to server {preferences.ServerIp} {preferences.ServerPort}", 0);
        }

        public DirectoryDigest GetDirectoryDigest(string directory, int recursionLevel = int.MaxValue)
        {
            GetDirectoryDigestCommand getCommand = new GetDirectoryDigestCommand(directory, recursionLevel);
            BlockShareCommand.WriteToClient(getCommand, networkStream, clientNetStat);

            Log($"Requested digest for [{directory}]", 0);

            //SetEntryTypeCommand setEntryTypeCommand = (SetEntryTypeCommand)BlockShareCommand.ReadFromClient(networkStream, clientNetStat, 10000);
            SetDirectoryDigestCommand setDirectoryDigestCommand;
            BlockShareCommand response = BlockShareCommand.ReadFromClient(networkStream, clientNetStat, 0);
            if(response.CommandType == BlockShareCommandType.InvalidOperation)
            {
                Log("Server refused to serve this operation", 0);
                return null;
            }
            else if(response.CommandType == BlockShareCommandType.SetDirectoryDigest)
            {
                setDirectoryDigestCommand = (SetDirectoryDigestCommand)response;
            }
            else
            {
                Log($"Unexpected command {response.CommandType} from server. Aborting", 0);
                return null;
            }
            string xmlDigest = setDirectoryDigestCommand.XmlPayload;

            DirectoryDigest directoryDigest = DirectoryDigest.FromXmlString(xmlDigest);

            return directoryDigest;
        }

        private void NetworkRead(Stream networkStream, byte[] data, int offset, int length, long timeout)
        {
            Utils.ReadPackage(networkStream, data, offset, length, timeout);
            clientNetStat.TotalReceived += (ulong)length;
        }

        private void OnHashListGeneratorProgress(Stream stream, double progress)
        {
            FileStream fileStream = (FileStream)stream;
            string fileName = fileStream.Name;
            OnHashingProgressChanged?.Invoke(this, fileName, progress);
        }

        private void OnHashListGeneratorFinished(Stream stream)
        {
            FileStream fileStream = (FileStream)stream;
            string fileName = fileStream.Name;
            OnHashingFinished?.Invoke(this, fileName);
        }

        private void DownloadFileNoHashlist(Stream networkStream, string fileName, int index, FileDigest fileDigest)
        {
            string localFilePath = Path.Combine(preferences.ClientStoragePath, fileName);

            FileInfo localFileInfo = new FileInfo(localFilePath);
            DirectoryInfo rootDirectoryInfo = new DirectoryInfo(preferences.ClientStoragePath);

            Utils.EnsurePathExists(rootDirectoryInfo, localFileInfo, preferences);

            if (!File.Exists(localFileInfo.FullName))
            {
                File.Create(localFileInfo.FullName).Close();
            }
            else if (fileDigest != null && localFileInfo.Length == fileDigest.Size)
            {
                Log($"Downloading finished", 1);
                return;
            }            

            using (FileStream localFileStream = new FileStream(localFileInfo.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                if (fileDigest == null)
                {
                    GetFileDigestCommand getFileInfoCommand = new GetFileDigestCommand(fileName);
                    BlockShareCommand.WriteToClient(getFileInfoCommand, networkStream, clientNetStat);

                    SetFileDigestCommand setFileDigestCommand = BlockShareCommand.ReadFromClient<SetFileDigestCommand>(networkStream, clientNetStat, 1000);

                    fileDigest = setFileDigestCommand.FileDigest;
                }
                long fileLength = fileDigest.Size;
                long blocksCount = fileLength / preferences.BlockSize;
                if (fileLength % preferences.BlockSize != 0)
                {
                    blocksCount++;
                }

                if (fileLength != localFileStream.Length)
                {
                    byte[] blockBytes = new byte[preferences.BlockSize];

                    long downloadStartIndex = (localFileStream.Length / preferences.BlockSize);
                    long blocksLeft = blocksCount - downloadStartIndex;

                    GetBlockRangeCommand getBlockRangeCommand = new GetBlockRangeCommand(fileName, downloadStartIndex, blocksLeft);
                    BlockShareCommand.WriteToClient(getBlockRangeCommand, networkStream, clientNetStat);

                    for (long j = downloadStartIndex; j < downloadStartIndex + blocksLeft; j++)
                    {
                        long filePos = j * preferences.BlockSize;
                        long bytesLeft = fileLength - filePos;
                        int blockSize;
                        if (bytesLeft > preferences.BlockSize)
                        {
                            blockSize = (int)preferences.BlockSize;
                        }
                        else
                        {
                            blockSize = (int)bytesLeft;
                        }

                        NetworkRead(networkStream, blockBytes, 0, blockSize, 0);
                        clientNetStat.Payload += (ulong)blockSize;

                        localFileStream.Seek(filePos, SeekOrigin.Begin);
                        localFileStream.Write(blockBytes, 0, blockSize);

                        DownloadingProgressEventData eventData = new DownloadingProgressEventData()
                        {
                            FileName = fileName,
                            RemoteHashList = null,
                            LocalHashList = null,
                            BlocksCount = (int)blocksCount,
                            DownloadedBlockIndex = (int)j
                        };
                        OnBlockDownloaded?.Invoke(this, eventData);
                    }
                }
            }

            Log($"Downloading finished", 1);
        }

        private void DownloadFileWithHashlist(Stream networkStream, string fileName, int index, FileDigest fileDigest)
        {
            string localFilePath = Path.Combine(preferences.ClientStoragePath, fileName);
            string localFileHashlistPath = preferences.HashMapper.GetHashpartFile(localFilePath);

            if (!File.Exists(localFilePath) && File.Exists(localFileHashlistPath))
            {
                File.WriteAllBytes(localFileHashlistPath, new byte[0]);
            }

            FileInfo localFileInfo = new FileInfo(localFilePath);
            DirectoryInfo rootDirectoryInfo = new DirectoryInfo(preferences.ClientStoragePath);

            Utils.EnsurePathExists(rootDirectoryInfo, localFileInfo, preferences);

            if (!File.Exists(localFileInfo.FullName))
            {
                File.Create(localFileInfo.FullName).Close();
            }

            using (FileStream localFileStream = new FileStream(localFileInfo.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (FileStream localFileHashlistStream =
                new FileStream(localFileHashlistPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                FileHashList localHashList = new FileHashList(localFileHashlistStream, preferences);
                if (localHashList.BlocksCount == 0)
                {
                    Log($"Local hashpart file is empty or does not exist, rehashing...", 2);
                    localHashList = FileHashListGenerator.GenerateHashList(localFileStream, localFileHashlistStream,
                         preferences, OnHashListGeneratorProgress, OnHashListGeneratorFinished);

                    localHashList.Flush();
                }
                else
                {
                    Log($"{localHashList.BlocksCount} hashes deserialized from local hashpart file", 2);
                }

                GetHashlistCommand getHashlistCommand = new GetHashlistCommand(fileName);
                BlockShareCommand.WriteToClient(getHashlistCommand, networkStream, clientNetStat);

                SetHashlistCommand setHashlistCommand = BlockShareCommand.ReadFromClient<SetHashlistCommand>(networkStream, clientNetStat, 0);
                FileHashList remoteHashList = FileHashList.Deserialise(setHashlistCommand.HashlistSerialized, null, preferences);
                Log($"Hashlist blocks count: {remoteHashList.BlocksCount}", 2);

                byte[] blockBytes = new byte[preferences.BlockSize];

                for (int i = 0; i < remoteHashList.BlocksCount; i++)
                {
                    FileHashBlock remoteBlock = remoteHashList[i];
                    FileHashBlock localBlock = localHashList[i];
                    if (remoteBlock == localBlock)
                    {
                        continue;
                    }

                    int requestStartIndex = i;
                    int requestBlocksNumber = 1;
                    for (int j = i + 1; j < remoteHashList.BlocksCount; j++)
                    {
                        remoteBlock = remoteHashList[j];
                        localBlock = localHashList[j];

                        bool isLocalBlockOk = remoteBlock == localBlock;
                        if (!isLocalBlockOk)
                        {
                            requestBlocksNumber++;
                        }
                    }

                    i += requestBlocksNumber;

                    Log(
                        $"Requesting range {requestStartIndex}-{requestStartIndex + requestBlocksNumber}: {remoteBlock}", 1);
 
                    GetBlockRangeCommand getBlockRangeCommand = new GetBlockRangeCommand(fileName, requestStartIndex, requestBlocksNumber);
                    BlockShareCommand.WriteToClient(getBlockRangeCommand, networkStream, clientNetStat);
                    for (int j = requestStartIndex; j < requestStartIndex + requestBlocksNumber; j++)
                    {
                        remoteBlock = remoteHashList[j];
                        long filePos = j * preferences.BlockSize;
                        long bytesLeft = setHashlistCommand.FileLength - filePos;
                        int blockSize;
                        if (bytesLeft > preferences.BlockSize)
                        {
                            blockSize = (int)preferences.BlockSize;
                        }
                        else
                        {
                            blockSize = (int)bytesLeft;
                        }

                        NetworkRead(networkStream, blockBytes, 0, blockSize, 0);
                        clientNetStat.Payload += (ulong)blockSize;

                        bool doSaveBlock = false;

                        if (preferences.ClientBlockVerificationEnabled)
                        {
                            FileHashBlock receivedBlock =
                                FileHashListGenerator.CalculateBlock(blockBytes, 0, blockSize, preferences, j);
                            if (receivedBlock == remoteBlock)
                            {
                                localHashList[j] = receivedBlock;
                                localHashList.Flush(j);
                                doSaveBlock = true;
                            }
                            else
                            {
                                Log($"Received erroneus block: {Utils.PrintHex(blockBytes, 0, 16)}", 0);
                            }                            
                        }
                        else
                        {
                            localHashList[j] = remoteBlock;
                            // MAY BE NO FLUSHING HERE, BLOCK IS NOT VERIFIED!
                            localHashList.Flush(j);                            
                            doSaveBlock = true;         
                        }

                        if (doSaveBlock)
                        {
                            localFileStream.Seek(filePos, SeekOrigin.Begin);
                            localFileStream.Write(blockBytes, 0, blockSize);
                            DownloadingProgressEventData eventData = new DownloadingProgressEventData()
                            {
                                FileName = fileName,
                                RemoteHashList = remoteHashList,
                                LocalHashList = localHashList,
                                BlocksCount = remoteHashList.BlocksCount,
                                DownloadedBlockIndex = j
                            };
                            OnBlockDownloaded?.Invoke(this, eventData);
                            //downloadProgress?.ReportProgress(this, (double)j / remoteHashList.BlocksCount, jobId);
                        }
                    }
                }
            }

            Log($"Downloading finished", 0);
        }

        private void DownloadFileInternal(Stream networkStream, string fileName, int index, FileDigest fileDigest)
        {
            if(preferences.UseHashLists)
            {
                DownloadFileWithHashlist(networkStream, fileName, index, fileDigest);
            }
            else
            {
                DownloadFileNoHashlist(networkStream, fileName, index, fileDigest);
            }
        }

        public void DownloadFile(string entryName)
        {
            GetEntryTypeCommand getEntryTypeCommand = new GetEntryTypeCommand(entryName);
            BlockShareCommand.WriteToClient(getEntryTypeCommand, networkStream, clientNetStat);
            Log($"Requested entry {entryName}", 0);

            SetEntryTypeCommand setEntryTypeCommand = BlockShareCommand.ReadFromClient<SetEntryTypeCommand>(networkStream, clientNetStat, 10000);

            FileSystemEntryType entryType = setEntryTypeCommand.EntryType;

            switch (entryType)
            {
                case FileSystemEntryType.NonExistent:
                    Log("Server refused to send requested entry, because it does not exist", 0);
                    return;
                case FileSystemEntryType.File:
                    Log("Server reported, requested entry is a file", 0);
                    break;
                case FileSystemEntryType.Directory:
                    Log("Server reported, requested entry is a directory", 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entryType), "Unknown File System Entry Type");
            }

            if (entryType == FileSystemEntryType.Directory)
            {
                GetDirectoryDigestCommand getDirectoryDigestCommand = new GetDirectoryDigestCommand(entryName, int.MaxValue);
                BlockShareCommand.WriteToClient(getDirectoryDigestCommand, networkStream, clientNetStat);

                SetDirectoryDigestCommand setDirectoryDigestCommand = BlockShareCommand.ReadFromClient<SetDirectoryDigestCommand>(networkStream, clientNetStat, 0);
                DirectoryDigest directoryDigest = DirectoryDigest.FromXmlString(setDirectoryDigestCommand.XmlPayload);

                IReadOnlyList<FileDigest> allFiles = directoryDigest.GetFilesRecursive();
                Log($"Files to load: {allFiles.Count}", 0);
                for (var index = 0; index < allFiles.Count; index++)
                {
                    DownloadFileInternal(networkStream, allFiles[index].RelativePath, index, allFiles[index]);
                    OnDownloadingFinished?.Invoke(this, allFiles[index].RelativePath);
                    //downloadProgress?.ReportOverallProgress(this, (double)index / allFiles.Count);
                }
            }
            else
            {
                DownloadFileInternal(networkStream, entryName, 0, null);
            }

            OnDownloadingFinished?.Invoke(this, entryName);
        }

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    DisconnectCommand disconnectCommand = new DisconnectCommand();
                    //BlockShareCommand.WriteToClient(disconnectCommand, networkStream, clientNetStat);
                    tcpClient.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null                
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BlockShareClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion Dispose
    }
}
