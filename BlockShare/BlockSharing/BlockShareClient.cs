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
            using (FileStream localFileStream = new FileStream(localFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                FileHashList localHashList = FileHashListGenerator.GenerateHashList(localFileStream, preferences, localHashProgress);

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
                Utils.ReadPackage(networkStream, fileLengthBytes, 0, fileLengthBytes.Length);
                long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);
                Logger?.Log($"File length: {fileLength}");

                byte[] hashListLengthBytes = new byte[sizeof(int)];
                //networkStream.Read(hashListLengthBytes, 0, hashListLengthBytes.Length);
                Utils.ReadPackage(networkStream, hashListLengthBytes, 0, hashListLengthBytes.Length);
                int hashListLength = BitConverter.ToInt32(hashListLengthBytes, 0);                

                byte[] hashListBytes = new byte[hashListLength];
                //networkStream.Read(hashListBytes, 0, hashListLength);
                Utils.ReadPackage(networkStream, hashListBytes, 0, hashListLength);
                FileHashList remoteHashList = FileHashList.Deserialise(hashListBytes, Preferences.HashSize, (int)preferences.BlockSize);

                Logger?.Log($"Hashlist blocks count: {remoteHashList.BlocksCount}");

                byte[] blockBytes = new byte[preferences.BlockSize];

                for (int i = 0; i < remoteHashList.BlocksCount; i++)
                {
                    FileHashBlock remoteBlock = remoteHashList[i];
                    FileHashBlock localBlock = null;
                    if (localHashList.BlocksCount > i)
                    {
                        localBlock = localHashList[i];
                    }

                    bool isLocalBlockOk = remoteBlock == localBlock;
                    if (!isLocalBlockOk)
                    {
                        //Logger?.Log($"Requesting block {i}: {remoteBlock}");
                        byte[] blockRequestBytes = BitConverter.GetBytes(i);
                        networkStream.Write(blockRequestBytes, 0, blockRequestBytes.Length);
                        long filePos = i * preferences.BlockSize;
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

                        Utils.ReadPackage(networkStream, blockBytes, 0, blockSize);

                        bool doSaveBlock = false;

                        if (preferences.ClientBlockVerificationEnabled)
                        {
                            FileHashBlock receivedBlock = FileHashListGenerator.CalculateBlock(blockBytes, 0, blockSize, preferences, i);
                            if (receivedBlock == remoteBlock)
                            {
                                doSaveBlock = true;
                            }
                            else
                            {
                                Logger?.Log($"Received erroneus block: {Utils.PrintHex(blockBytes, 0, 16)}");
                                i--;
                                continue;
                            }
                        }
                        else
                        {
                            doSaveBlock = true;
                        }

                        if(doSaveBlock)
                        {
                            localFileStream.Seek(filePos, SeekOrigin.Begin);
                            localFileStream.Write(blockBytes, 0, blockSize);
                            downloadProgress?.ReportProgress(this, (double)i / remoteHashList.BlocksCount);
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
