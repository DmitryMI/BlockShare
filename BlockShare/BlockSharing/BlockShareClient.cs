using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    public class BlockShareClient
    {
        private TcpClient tcpClient;

        private Preferences preferences;

        public ILogger Logger { get; set; }

        public BlockShareClient(Preferences preferences, ILogger logger)
        {
            this.preferences = preferences;
            Logger = logger;
        }

        public void DownloadFile(string serverIp, int serverPort, string fileName, IProgressReporter localHashProgress, IProgressReporter downloadProgress)
        {
            string localFilePath = Path.Combine(preferences.ClientStoragePath, fileName);
            string localFileHashlistName = fileName + ".hashpart";
            string localFileHashlistPath = Path.Combine(preferences.ClientStoragePath, localFileHashlistName);
            using (FileStream localFileStream = new FileStream(localFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (FileStream localFileHashlistStream = new FileStream(localFileHashlistPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                FileHashList localHashList = new FileHashList(localFileHashlistStream, preferences);                
                if(localHashList.BlocksCount == 0)
                {
                    Logger?.Log($"Local hashpart file is empty or does not exist, rehashing...");
                    localHashList = FileHashListGenerator.GenerateHashList(localFileStream, localFileHashlistStream, preferences, localHashProgress);
                    localHashList.Flush();
                }
                else
                {
                    Logger?.Log($"{localHashList.BlocksCount} hashes deserialized from local hashpart file");
                }
                //FileHashList localHashList = FileHashListGenerator.GenerateHashList(localFileStream, preferences, localHashProgress);

                tcpClient = new TcpClient();
                tcpClient.Connect(serverIp, serverPort);

                Logger?.Log($"Connected to server {serverIp} {serverPort}");

                NetworkStream networkStream = tcpClient.GetStream();

                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                int fileNameLength = fileNameBytes.Length;
                byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameLength);

                networkStream.Write(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                networkStream.Write(fileNameBytes, 0, fileNameBytes.Length);

                Logger?.Log($"Requested file {fileName}");

                byte[] fileLengthBytes = new byte[sizeof(long)];
                //networkStream.Read(fileLengthBytes, 0, fileLengthBytes.Length);
                Utils.ReadPackage(networkStream, fileLengthBytes, 0, fileLengthBytes.Length, 0);
                long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);
                Logger?.Log($"File length: {fileLength}");

                byte[] hashListLengthBytes = new byte[sizeof(int)];
                //networkStream.Read(hashListLengthBytes, 0, hashListLengthBytes.Length);
                Utils.ReadPackage(networkStream, hashListLengthBytes, 0, hashListLengthBytes.Length, 10000);
                int hashListLength = BitConverter.ToInt32(hashListLengthBytes, 0);                

                byte[] hashListBytes = new byte[hashListLength];
                //networkStream.Read(hashListBytes, 0, hashListLength);
                Utils.ReadPackage(networkStream, hashListBytes, 0, hashListLength, 10000);
                FileHashList remoteHashList = FileHashList.Deserialise(hashListBytes, null, preferences);

                Logger?.Log($"Hashlist blocks count: {remoteHashList.BlocksCount}");

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

                    Logger?.Log($"Requesting range {requestStartIndex}-{requestStartIndex + requestBlocksNumber}: {remoteBlock}");
                    byte[] requestStartIndexBytes = BitConverter.GetBytes(requestStartIndex);
                    networkStream.Write(requestStartIndexBytes, 0, requestStartIndexBytes.Length);
                    byte[] requestBlocksNumberBytes = BitConverter.GetBytes(requestBlocksNumber);
                    networkStream.Write(requestBlocksNumberBytes, 0, requestBlocksNumberBytes.Length);

                    for (int j = requestStartIndex; j < requestStartIndex + requestBlocksNumber; j++)
                    {
                        remoteBlock = remoteHashList[j];
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

                        Utils.ReadPackage(networkStream, blockBytes, 0, blockSize, 0);

                        bool doSaveBlock = false;

                        if (preferences.ClientBlockVerificationEnabled)
                        {
                            FileHashBlock receivedBlock = FileHashListGenerator.CalculateBlock(blockBytes, 0, blockSize, preferences, j);
                            if (receivedBlock == remoteBlock)
                            {
                                doSaveBlock = true;
                            }
                            else
                            {
                                Logger?.Log($"Received erroneus block: {Utils.PrintHex(blockBytes, 0, 16)}");                                
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
                            downloadProgress?.ReportProgress(this, (double)j / remoteHashList.BlocksCount);
                        }
                    }
                }
            }
            downloadProgress?.ReportFinishing(this, true);
            Logger?.Log($"File downloading finished");
            tcpClient.Close();
        }
    }    
}
