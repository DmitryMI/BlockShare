using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    public class BlockShareServer
    {
        private TcpListener tcpListener;

        private Preferences preferences;

        private IPEndPoint localEndpoint;

        public IProgressReporter HashListGeneratorReporter { get; set; }

        public ILogger Logger { get; set; }

        private Task worker;

        public BlockShareServer(string ip, int port, Preferences preferences, IProgressReporter hashProgress, ILogger logger)
        {
            HashListGeneratorReporter = hashProgress;
            this.preferences = preferences;
            Logger = logger;
            localEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void StartServer()
        {
            Logger?.Log($"Starting server on {localEndpoint}...");
            tcpListener = new TcpListener(localEndpoint);
            tcpListener.Start();

            worker = new Task(WorkingMethod);
            worker.Start();

            Logger?.Log($"Server is now listening on {localEndpoint}");
        }

        private void WorkingMethod()
        {
            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                Task task = new Task(ClientAcceptedMethod, client);
                task.Start();
            }
        }

        private void ClientAcceptedMethod(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;

            Logger?.Log($"Client accepted from {client.Client.RemoteEndPoint}");
            NetworkStream networkStream = client.GetStream();
            Logger?.Log($"Waiting for file request from {client.Client.RemoteEndPoint}...");

            byte[] fileNameLengthBytes = new byte[sizeof(int)];

            Utils.ReadPackage(networkStream, fileNameLengthBytes, 0, sizeof(int), 10000);
            int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);
            byte[] fileNameBytes = new byte[fileNameLength];

            Utils.ReadPackage(networkStream, fileNameBytes, 0, fileNameLength, 10000);
            string fileName = Encoding.UTF8.GetString(fileNameBytes);
            string fileHashListName = fileName + ".hashlist";
            string filePath = Path.Combine(preferences.ServerStoragePath, fileName);
            if (!File.Exists(filePath))
            {
                client.Close();
                return;
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {                
                byte[] hashListSerialized = null;

                Logger.Log($"On-server file size: {fileStream.Length}");

                string fileHashListPath = Path.Combine(preferences.ServerStoragePath, fileHashListName);
                FileHashList hashList;
                if (!File.Exists(fileHashListPath))
                {
                    Logger?.Log($"Calculating hash list for file {filePath} by request of {client.Client.RemoteEndPoint}...");
                    hashList = FileHashListGenerator.GenerateHashList(fileStream, null, preferences, HashListGeneratorReporter);
                    hashListSerialized = hashList.Serialize();
                    using (FileStream fileHashListStream = new FileStream(fileHashListPath, FileMode.CreateNew, FileAccess.Write))
                    {
                        fileHashListStream.Write(hashListSerialized, 0, hashListSerialized.Length);
                    }
                }
                else
                {
                    Logger?.Log($"Reading hash list of file {filePath} by request of {client.Client.RemoteEndPoint}...");
                    using (FileStream fileHashListStream = new FileStream(fileHashListPath, FileMode.Open, FileAccess.Read))
                    {
                        hashListSerialized = new byte[fileHashListStream.Length];
                        fileHashListStream.Read(hashListSerialized, 0, (int)fileHashListStream.Length);
                    }                    
                }

                byte[] fileLengthBytes = BitConverter.GetBytes(fileStream.Length);
                networkStream.Write(fileLengthBytes, 0, fileLengthBytes.Length);

                byte[] hashListLengthBytes = BitConverter.GetBytes(hashListSerialized.Length);
                networkStream.Write(hashListLengthBytes, 0, hashListLengthBytes.Length);

                networkStream.Write(hashListSerialized, 0, hashListSerialized.Length);
                Logger?.Log($"Hash list for file {filePath} sent to {client.Client.RemoteEndPoint}.");

                byte[] blockRequestBytes = new byte[sizeof(int)];
                byte[] blockBytes = new byte[preferences.BlockSize];
                while (client.Connected)
                {
                    //networkStream.Read(blockRequestBytes, 0, blockRequestBytes.Length);
                    Utils.ReadPackage(networkStream, blockRequestBytes, 0, blockRequestBytes.Length, 60000);
                    int requestStartIndex = BitConverter.ToInt32(blockRequestBytes, 0);
                    Utils.ReadPackage(networkStream, blockRequestBytes, 0, blockRequestBytes.Length, 10000);
                    int requestBlocksNumber = BitConverter.ToInt32(blockRequestBytes, 0);
                    for (int i = requestStartIndex; i < requestStartIndex + requestBlocksNumber; i++)
                    {
                        int filePosition = (int)(i * preferences.BlockSize);
                        fileStream.Seek(filePosition, SeekOrigin.Begin);
                        int blockSize;
                        long bytesLeft = fileStream.Length - filePosition;
                        if (bytesLeft > preferences.BlockSize)
                        {
                            blockSize = (int)preferences.BlockSize;
                        }
                        else
                        {
                            blockSize = (int)bytesLeft;
                        }
                        fileStream.Read(blockBytes, 0, blockSize);
                        FileHashBlock localBlock = FileHashListGenerator.CalculateBlock(blockBytes, 0, blockSize, preferences, i);
                        networkStream.Write(blockBytes, 0, blockSize);
                    }

#if DEBUG
                    //Logger?.Log($"Block {blockRequest} <{Utils.PrintHex(blockBytes, 0, 16)}> of {filePath} sent to {client.Client.RemoteEndPoint}.");
#endif
                }

                Logger?.Log($"Client {client.Client.RemoteEndPoint} disconnected");
            }            

        }
    }
}
