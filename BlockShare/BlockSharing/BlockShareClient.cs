using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BlockShare.BlockSharing.DirectoryDigesting;
using BlockShare.BlockSharing.RemoteFileSystem;
using Microsoft.SqlServer.Server;

namespace BlockShare.BlockSharing
{
    public class BlockShareClient
    {
        private TcpClient tcpClient;

        private Preferences preferences;

        private NetStat clientNetStat = new NetStat();
        public NetStat GetClientNetStat => clientNetStat.CloneNetStat();

        public ILogger Logger { get; set; }

        public BlockShareClient(Preferences preferences, ILogger logger)
        {
            this.preferences = preferences;
            Logger = logger;
        }

        private void NetworkWrite(NetworkStream stream, byte[] data, int offset, int length)
        {
            stream.Write(data, offset, length);
            clientNetStat.TotalSent += (ulong)length;
        }

        private void NetworkRead(NetworkStream stream, byte[] data, int offset, int length, long timeout)
        {
            Utils.ReadPackage(stream, data, offset, length, timeout);
            clientNetStat.TotalReceived += (ulong)length;
        }

        private void EnsurePathExists(DirectoryInfo rootDirInfo, FileInfo fileInfo)
        {
            Stack<DirectoryInfo> pathStack = new Stack<DirectoryInfo>();
            DirectoryInfo parent = fileInfo.Directory;

            while (parent != null && !Utils.ArePathsEqual(parent.FullName, rootDirInfo.FullName))
            {
                pathStack.Push(parent);
                parent = parent.Parent;
            }

            while (pathStack.Count > 0)
            {
                DirectoryInfo dir = pathStack.Pop();
                if (!Directory.Exists(dir.FullName))
                {
                    Directory.CreateDirectory(dir.FullName);
                    Log($"Created directory: {dir.FullName}", 3);
                }
            }
        }

        private void Log(string message, int withVerbosity)
        {
            if (withVerbosity <= preferences.Verbosity)
            {
                Logger?.Log(message);
            }
        }

        private void DownloadFileInternal(NetworkStream networkStream, string fileName, IProgressReporter localHashProgress, IProgressReporter downloadProgress, int jobId)
        {
            //string localFileHashlistName = fileName + Preferences.HashpartExtension;
            //string localFileHashlistPath = Path.Combine(preferences.ClientStoragePath, localFileHashlistName);

            string localFilePath = Path.Combine(preferences.ClientStoragePath, fileName);
            string localFileHashlistPath = preferences.HashMapper.GetHashpartFile(localFilePath);

            if (!File.Exists(localFilePath) && File.Exists(localFileHashlistPath))
            {
                File.WriteAllBytes(localFileHashlistPath, new byte[0]);
            }

            FileInfo localFileInfo = new FileInfo(localFilePath);
            DirectoryInfo rootDirectoryInfo = new DirectoryInfo(preferences.ClientStoragePath);

            EnsurePathExists(rootDirectoryInfo, localFileInfo);

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
                        preferences, localHashProgress);
                    localHashList.Flush();
                }
                else
                {
                    Log($"{localHashList.BlocksCount} hashes deserialized from local hashpart file", 2);
                }


                byte[] fileLengthBytes = new byte[sizeof(long)];
                //networkStream.Read(fileLengthBytes, 0, fileLengthBytes.Length);
                NetworkRead(networkStream, fileLengthBytes, 0, fileLengthBytes.Length, 0);
                long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);
                Log($"File length: {fileLength}", 2);

                byte[] hashListLengthBytes = new byte[sizeof(int)];
                //networkStream.Read(hashListLengthBytes, 0, hashListLengthBytes.Length);
                NetworkRead(networkStream, hashListLengthBytes, 0, hashListLengthBytes.Length, 10000);
                int hashListLength = BitConverter.ToInt32(hashListLengthBytes, 0);

                byte[] hashListBytes = new byte[hashListLength];
                //networkStream.Read(hashListBytes, 0, hashListLength);
                NetworkRead(networkStream, hashListBytes, 0, hashListLength, 10000);
                FileHashList remoteHashList = FileHashList.Deserialise(hashListBytes, null, preferences);

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
                    NetworkWrite(networkStream, new byte[] { (byte)Command.NextBlock }, 0, 1);
                    byte[] requestStartIndexBytes = BitConverter.GetBytes(requestStartIndex);
                    NetworkWrite(networkStream, requestStartIndexBytes, 0, requestStartIndexBytes.Length);
                    byte[] requestBlocksNumberBytes = BitConverter.GetBytes(requestBlocksNumber);
                    NetworkWrite(networkStream, requestBlocksNumberBytes, 0, requestBlocksNumberBytes.Length);

                    for (int j = requestStartIndex; j < requestStartIndex + requestBlocksNumber; j++)
                    {
                        remoteBlock = remoteHashList[j];
                        long filePos = j * preferences.BlockSize;
                        long bytesLeft = fileLength - filePos;
                        int blockSize;
                        if (bytesLeft > preferences.BlockSize)
                        {
                            blockSize = (int) preferences.BlockSize;
                        }
                        else
                        {
                            blockSize = (int) bytesLeft;
                        }

                        NetworkRead(networkStream, blockBytes, 0, blockSize, 0);
                        clientNetStat.Payload += (ulong) blockSize;

                        bool doSaveBlock = false;

                        if (preferences.ClientBlockVerificationEnabled)
                        {
                            FileHashBlock receivedBlock =
                                FileHashListGenerator.CalculateBlock(blockBytes, 0, blockSize, preferences, j);
                            if (receivedBlock == remoteBlock)
                            {
                                doSaveBlock = true;
                            }
                            else
                            {
                                Log($"Received erroneus block: {Utils.PrintHex(blockBytes, 0, 16)}", 0);
                            }

                            localHashList[j] = receivedBlock;
                            localHashList.Flush(j);
                        }
                        else
                        {
                            doSaveBlock = true;
                        }

                        if (doSaveBlock)
                        {
                            localFileStream.Seek(filePos, SeekOrigin.Begin);
                            localFileStream.Write(blockBytes, 0, blockSize);
                            downloadProgress?.ReportProgress(this, (double) j / remoteHashList.BlocksCount, jobId);
                        }
                    }
                }
            }

            NetworkWrite(networkStream, new byte[] { (byte)Command.Terminate }, 0, 1);
            downloadProgress?.ReportFinishing(this, true, jobId);
            Log($"Downloading finished", 0);
        }

        public void DownloadFile(string serverIp, int serverPort, string fileName, IProgressReporter localHashProgress,
            IProgressReporter downloadProgress)
        {

            tcpClient = new TcpClient();
            tcpClient.Connect(serverIp, serverPort);

            Log($"Connected to server {serverIp} {serverPort}", 0);

            NetworkStream networkStream = tcpClient.GetStream();

            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            int fileNameLength = fileNameBytes.Length;
            byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameLength);

            NetworkWrite(networkStream, fileNameLengthBytes, 0, fileNameLengthBytes.Length);
            NetworkWrite(networkStream, fileNameBytes, 0, fileNameBytes.Length);

            Log($"Requested {fileName}", 0);

            byte[] entryTypeMessage = new byte[1];
            NetworkRead(networkStream, entryTypeMessage, 0, entryTypeMessage.Length, 0);
            FileSystemEntryType entryType = (FileSystemEntryType) entryTypeMessage[0];
            switch (entryType)
            {
                case FileSystemEntryType.NonExistent:
                    Log("Server refused to send requested entry, because it does not exist", 0);
                    tcpClient.Close();
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
                byte[] digestSizeBytes = new byte[sizeof(int)];
                NetworkRead(networkStream, digestSizeBytes, 0, digestSizeBytes.Length, 0);
                int digestSize = BitConverter.ToInt32(digestSizeBytes, 0);
                Log($"Digest length: {digestSize}", 0);
                byte[] xmlDirectoryDigestBytes = new byte[digestSize];
                NetworkRead(networkStream, xmlDirectoryDigestBytes, 0, xmlDirectoryDigestBytes.Length, 0);
                
                /*
                string xmlDirectoryDigest = Encoding.UTF8.GetString(xmlDirectoryDigestBytes);
                Log($"Digest received", 0);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xmlDirectoryDigest);
                Log($"Digest parsed to xml-dom", 0);
                */

                DirectoryDigest directoryDigest = new DirectoryDigest(xmlDirectoryDigestBytes);

                //string[] fileNames = Utils.GetFileNamesFromDigest(xmlDocument);
                Log($"Files to load: {directoryDigest.Count}", 0);
                for (var index = 0; index < directoryDigest.Count; index++)
                {
                    //var name = fileNames[index];
                    FileDigest fileDigest = directoryDigest[index];
                    
                    fileNameBytes = Encoding.UTF8.GetBytes(fileDigest.RelativePath);
                    fileNameLength = fileNameBytes.Length;
                    fileNameLengthBytes = BitConverter.GetBytes(fileNameLength);

                    NetworkWrite(networkStream, fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                    NetworkWrite(networkStream, fileNameBytes, 0, fileNameBytes.Length);

                    Log($"Requested file {fileDigest.RelativePath}\nWaiting for entry type message...", 1);

                    entryTypeMessage = new byte[1];
                    NetworkRead(networkStream, entryTypeMessage, 0, entryTypeMessage.Length, 0);
                    entryType = (FileSystemEntryType) entryTypeMessage[0];

                    if (entryType != FileSystemEntryType.File)
                    {
                        throw new ArgumentException("Error: Server reported, requested entry is not a file");
                    }

                    Log($"Entry type: {entryType}", 2);

                    DownloadFileInternal(networkStream, fileDigest.RelativePath, localHashProgress, downloadProgress, index);

                    downloadProgress?.ReportOverallProgress(this, (double) index / directoryDigest.Count);
                }

                return;
            }

            DownloadFileInternal(networkStream, fileName, localHashProgress, downloadProgress, 0);
            
            tcpClient.Close();
        }

        public RemoteFileSystemViewer Browse(string serverIp, int serverPort, string directory)
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(serverIp, serverPort);

            Log($"Connected to server {serverIp} {serverPort}", 0);

            NetworkStream networkStream = tcpClient.GetStream();

            byte[] fileNameBytes = Encoding.UTF8.GetBytes(directory);
            int fileNameLength = fileNameBytes.Length;
            byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameLength);

            NetworkWrite(networkStream, fileNameLengthBytes, 0, fileNameLengthBytes.Length);
            NetworkWrite(networkStream, fileNameBytes, 0, fileNameBytes.Length);

            Log($"Requested {directory}", 0);

            byte[] entryTypeMessage = new byte[1];
            NetworkRead(networkStream, entryTypeMessage, 0, entryTypeMessage.Length, 0);
            FileSystemEntryType entryType = (FileSystemEntryType) entryTypeMessage[0];
            switch (entryType)
            {
                case FileSystemEntryType.NonExistent:
                    Log("Server refused to send requested entry, because it does not exist", 0);
                    tcpClient.Close();
                    return null;
                case FileSystemEntryType.File:
                    Log("Server reported, requested entry is a file", 0);
                    
                    tcpClient.Close();
                    return null;
                case FileSystemEntryType.Directory:
                    Log("Server reported, requested entry is a directory", 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entryType), "Unknown File System Entry Type");
            }

            byte[] digestSizeBytes = new byte[sizeof(int)];
            NetworkRead(networkStream, digestSizeBytes, 0, digestSizeBytes.Length, 0);
            int digestSize = BitConverter.ToInt32(digestSizeBytes, 0);
            Log($"Digest length: {digestSize}", 0);
            byte[] xmlDirectoryDigestBytes = new byte[digestSize];
            NetworkRead(networkStream, xmlDirectoryDigestBytes, 0, xmlDirectoryDigestBytes.Length, 0);
            
            /*            
            string xmlDirectoryDigest = Encoding.UTF8.GetString(xmlDirectoryDigestBytes);
            Log($"Digest received", 0);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlDirectoryDigest);
            Log($"Digest parsed to xml-dom", 0);
            */
            tcpClient.Close();

            DirectoryDigest directoryDigest = new DirectoryDigest(xmlDirectoryDigestBytes);

            //string[] fileNames = Utils.GetFileNamesFromDigest(xmlDocument);

            return new RemoteFileSystemViewer(directoryDigest);
        }
    }    
}
