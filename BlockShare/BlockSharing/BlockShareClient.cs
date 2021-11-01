using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands;
using BlockShare.BlockSharing.DirectoryDigesting;
using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    class BlockShareClient : IDisposable
    {
        private TcpClient tcpClient;

        private Preferences preferences;

        private NetStat clientNetStat = new NetStat();
        private bool disposedValue;

        public NetStat GetClientNetStat => clientNetStat.CloneNetStat();

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

        public BlockShareClient(Preferences preferences, ILogger logger)
        {
            this.preferences = preferences;
            Logger = logger;

            Log($"Connecting to server {preferences.ServerIp} {preferences.ServerPort}...", 0);

            tcpClient = new TcpClient();
            tcpClient.Connect(preferences.ServerIp, preferences.ServerPort);

            Log($"Connected to server {preferences.ServerIp} {preferences.ServerPort}", 0);
        }

        public DirectoryDigest GetDirectoryDigest(string directory, int recursionLevel = int.MaxValue)
        {
            GetDirectoryDigestCommand getCommand = new GetDirectoryDigestCommand(directory, recursionLevel);
            BlockShareCommand.WriteToClient(getCommand, tcpClient, clientNetStat);

            Log($"Requested digest for [{directory}]", 0);

            //SetEntryTypeCommand setEntryTypeCommand = (SetEntryTypeCommand)BlockShareCommand.ReadFromClient(tcpClient, clientNetStat, 10000);
            SetDirectoryDigestCommand setDirectoryDigestCommand;
            BlockShareCommand response = BlockShareCommand.ReadFromClient(tcpClient, clientNetStat, 0);
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

        private void NetworkRead(TcpClient tcpClient, byte[] data, int offset, int length, long timeout)
        {
            NetworkStream stream = tcpClient.GetStream();
            Utils.ReadPackage(tcpClient, stream, data, offset, length, timeout);
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

        private void DownloadFileNoHashlist(string fileName, int index)
        {
            string localFilePath = Path.Combine(preferences.ClientStoragePath, fileName);

            FileInfo localFileInfo = new FileInfo(localFilePath);
            DirectoryInfo rootDirectoryInfo = new DirectoryInfo(preferences.ClientStoragePath);

            Utils.EnsurePathExists(rootDirectoryInfo, localFileInfo, preferences);

            if (!File.Exists(localFileInfo.FullName))
            {
                File.Create(localFileInfo.FullName).Close();
            }

            using (FileStream localFileStream = new FileStream(localFileInfo.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                //GetHashlistCommand getHashlistCommand = new GetHashlistCommand(fileName);
                //BlockShareCommand.WriteToClient(getHashlistCommand, tcpClient, clientNetStat);
                GetFileInfoCommand getFileInfoCommand = new GetFileInfoCommand(fileName);
                BlockShareCommand.WriteToClient(getFileInfoCommand, tcpClient, clientNetStat);

                //SetHashlistCommand setHashlistCommand = BlockShareCommand.ReadFromClient<SetHashlistCommand>(tcpClient, clientNetStat, 0);
                //FileHashList remoteHashList = FileHashList.Deserialise(setHashlistCommand.HashlistSerialized, null, preferences);
                //Log($"Hashlist blocks count: {remoteHashList.BlocksCount}", 2);
                SetFileInfoCommand setFileInfoCommand = BlockShareCommand.ReadFromClient<SetFileInfoCommand>(tcpClient, clientNetStat, 1000);

                long fileLength = setFileInfoCommand.FileLength;
                long blocksCount = fileLength / preferences.BlockSize;
                if (fileLength % preferences.BlockSize != 0)
                {
                    blocksCount++;
                }

                byte[] blockBytes = new byte[preferences.BlockSize];

                long downloadStartIndex = (localFileStream.Length / preferences.BlockSize);
                long blocksLeft = blocksCount - downloadStartIndex;

                GetBlockRangeCommand getBlockRangeCommand = new GetBlockRangeCommand(fileName, downloadStartIndex, blocksLeft);
                BlockShareCommand.WriteToClient(getBlockRangeCommand, tcpClient, clientNetStat);

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

                    NetworkRead(tcpClient, blockBytes, 0, blockSize, 0);
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

            Log($"Downloading finished", 0);
        }

        private void DownloadFileWithHashlist(string fileName, int index)
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
                BlockShareCommand.WriteToClient(getHashlistCommand, tcpClient, clientNetStat);

                SetHashlistCommand setHashlistCommand = BlockShareCommand.ReadFromClient<SetHashlistCommand>(tcpClient, clientNetStat, 0);
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
                    BlockShareCommand.WriteToClient(getBlockRangeCommand, tcpClient, clientNetStat);
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

                        NetworkRead(tcpClient, blockBytes, 0, blockSize, 0);
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

        private void DownloadFileInternal(string fileName, int index)
        {
            if(preferences.UseHashLists)
            {
                DownloadFileWithHashlist(fileName, index);
            }
            else
            {
                DownloadFileNoHashlist(fileName, index);
            }
        }

        public void DownloadFile(string entryName)
        {
            GetEntryTypeCommand getEntryTypeCommand = new GetEntryTypeCommand(entryName);
            BlockShareCommand.WriteToClient(getEntryTypeCommand, tcpClient, clientNetStat);
            Log($"Requested entry {entryName}", 0);

            SetEntryTypeCommand setEntryTypeCommand = BlockShareCommand.ReadFromClient<SetEntryTypeCommand>(tcpClient, clientNetStat, 10000);

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
                BlockShareCommand.WriteToClient(getDirectoryDigestCommand, tcpClient, clientNetStat);

                SetDirectoryDigestCommand setDirectoryDigestCommand = BlockShareCommand.ReadFromClient<SetDirectoryDigestCommand>(tcpClient, clientNetStat, 0);
                DirectoryDigest directoryDigest = DirectoryDigest.FromXmlString(setDirectoryDigestCommand.XmlPayload);

                IReadOnlyList<FileDigest> allFiles = directoryDigest.GetFilesRecursive();
                Log($"Files to load: {allFiles.Count}", 0);
                for (var index = 0; index < allFiles.Count; index++)
                {
                    DownloadFileInternal(allFiles[index].RelativePath, index);
                    OnDownloadingFinished?.Invoke(this, allFiles[index].RelativePath);
                    //downloadProgress?.ReportOverallProgress(this, (double)index / allFiles.Count);
                }
            }
            else
            {
                DownloadFileInternal(entryName, 0);
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
                    BlockShareCommand.WriteToClient(disconnectCommand, tcpClient, clientNetStat);
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
