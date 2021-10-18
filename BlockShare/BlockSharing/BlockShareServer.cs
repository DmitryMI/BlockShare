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

        private NetStat serverNetStat = new NetStat();
        private NetStat GetServerNetStat => serverNetStat.CloneNetStat();

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

        private void NetworkWrite(NetworkStream stream, byte[] data, int offset, int length)
        {
            stream.Write(data, offset, length);
            serverNetStat.TotalSent += (ulong) length;
        }

        private void NetworkRead(NetworkStream stream, byte[] data, int offset, int length, long timeout)
        {
            Utils.ReadPackage(stream, data, offset, length, timeout);
            serverNetStat.TotalReceived += (ulong) length;
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
        
        private bool CheckRequestValidity(string requestedEntry)
        {
            if (String.IsNullOrWhiteSpace(requestedEntry))
            {
                Logger?.Log("Requested Entry is empty");
                return false;
            }

            DirectoryInfo parent = null;
            FileSystemInfo entryInfo = null;
            if (File.Exists(requestedEntry))
            {
                FileInfo fileInfo = new FileInfo(requestedEntry);
                entryInfo = fileInfo;
                parent = fileInfo.Directory;
            }
            else if (Directory.Exists(requestedEntry))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(requestedEntry);
                entryInfo = directoryInfo;
                parent = directoryInfo.Parent;
            }
            else
            {
                Logger?.Log($"Error: file or directory {requestedEntry} does not exist");
                return false;
            }
            DirectoryInfo rootDirectoryInfo = new DirectoryInfo(preferences.ServerStoragePath);
            bool isInsideRoot;
            if (entryInfo.FullName == rootDirectoryInfo.FullName)
            {
                isInsideRoot = true;
            }
            else if (parent == null)
            {
                isInsideRoot = false;
            }
            else
            {
                while (parent != null && parent.FullName != rootDirectoryInfo.FullName)
                {
                    parent = parent.Parent;
                }

                isInsideRoot = parent != null;
            }

            if (!isInsideRoot)
            {
                Logger?.Log($"Security error: {entryInfo.FullName} is not inside Server Folder");
                return false;
            }

            return true;
        }

        private string PathCombineWorkaround(string p1, string p2)
        {
            p1 = p1.Replace('/', Path.DirectorySeparatorChar);
            p2 = p2.Replace('/', Path.DirectorySeparatorChar);

            if (p2[0] == Path.DirectorySeparatorChar)
            {
                if (p1[p1.Length - 1] == Path.DirectorySeparatorChar)
                {
                    p1 = p1.Remove(p1.Length - 1, 1);
                }
                return p1 + p2;
            }
            else
            {
                if (p1[p1.Length - 1] == Path.DirectorySeparatorChar)
                {
                    return p1 + p2;
                }

                return p1 + Path.DirectorySeparatorChar + p2;
            }
        }

        private void ClientLoop(TcpClient client)
        {
            NetworkStream networkStream = client.GetStream();

            Logger?.Log($"Waiting for file request from {client.Client.RemoteEndPoint}...");

            byte[] fileNameLengthBytes = new byte[sizeof(int)];

            Utils.ReadPackage(networkStream, fileNameLengthBytes, 0, sizeof(int), 10000);

            int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);
            Logger?.Log($"Client request name length received: {fileNameLength}");
            byte[] fileNameBytes = new byte[fileNameLength];

            NetworkRead(networkStream, fileNameBytes, 0, fileNameLength, 10000);
            Logger?.Log($"Client request bytes received");

            string fileName = Encoding.UTF8.GetString(fileNameBytes);
            Logger?.Log($"Name decoded: {fileName}");
            string filePath = null;
            Logger?.Log($"Combining {preferences.ServerStoragePath} with {fileName}");

            try
            {
                filePath = Path.Combine(preferences.ServerStoragePath, fileName);
            }
            catch (ArgumentNullException argEx)
            {
                Logger?.Log("Argument was null: \n" + argEx.Message);
            }
            catch (ArgumentException ex)
            {
                Logger?.Log("Argument exception occured: \n" + ex.Message);

                // Stupid workaround
                filePath = PathCombineWorkaround(preferences.ServerStoragePath, fileName);
                Logger?.Log($"Using workaround for stupid Path.Combine: {filePath}");
            }

            Logger?.Log($"Client request received: {fileName}");

            byte[] entryTypeMessage;

            if (!CheckRequestValidity(filePath))
            {
                entryTypeMessage = new byte[] { (byte)FileSystemEntryType.NonExistent };
                NetworkWrite(networkStream, entryTypeMessage, 0, entryTypeMessage.Length);

                //client.Close();
                return;
                //continue;
            }

            if (Directory.Exists(filePath))
            {
                Logger?.Log("Directory was requested. Generating XML digest...");
                DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
                DirectoryInfo rootDirectoryInfo = new DirectoryInfo(preferences.ServerStoragePath);
                string xmlDigest = Utils.GenerateDirectoryDigest(directoryInfo, rootDirectoryInfo);

                Logger?.Log($"Digest generated. Length: {xmlDigest.Length}");

                byte[] xmlDigestBytes = Encoding.UTF8.GetBytes(xmlDigest);
                int digestLength = xmlDigestBytes.Length;
                byte[] digestLengthBytes = BitConverter.GetBytes(digestLength);

                entryTypeMessage = new byte[] { (byte)FileSystemEntryType.Directory };
                NetworkWrite(networkStream, entryTypeMessage, 0, entryTypeMessage.Length);
                Logger?.Log($"Entry type message sent: {FileSystemEntryType.Directory}");

                NetworkWrite(networkStream, digestLengthBytes, 0, digestLengthBytes.Length);
                Logger?.Log($"Digest Length sent");
                NetworkWrite(networkStream, xmlDigestBytes, 0, xmlDigestBytes.Length);
                Logger?.Log($"Digest sent.");

                //client.Close();
                //Logger?.Log($"Connection closed");
                return;
            }

            entryTypeMessage = new byte[] { (byte)FileSystemEntryType.File };
            NetworkWrite(networkStream, entryTypeMessage, 0, entryTypeMessage.Length);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                string fileHashListName = fileName + ".hashlist";
                Logger?.Log($"Hashlist file name: {fileHashListName}");

                byte[] hashListSerialized = null;

                Logger?.Log($"On-server file size: {fileStream.Length}");

                string fileHashListPath = Path.Combine(preferences.ServerStoragePath, fileHashListName);
                FileHashList hashList;
                if (!File.Exists(fileHashListPath))
                {
                    Logger?.Log(
                        $"Calculating hash list for file {filePath} by request of {client.Client.RemoteEndPoint}...");
                    hashList = FileHashListGenerator.GenerateHashList(fileStream, null, preferences,
                        HashListGeneratorReporter);
                    hashListSerialized = hashList.Serialize();
                    using (FileStream fileHashListStream =
                        new FileStream(fileHashListPath, FileMode.CreateNew, FileAccess.Write))
                    {
                        fileHashListStream.Write(hashListSerialized, 0, hashListSerialized.Length);
                    }
                }
                else
                {
                    Logger?.Log(
                        $"Reading hash list of file {filePath} by request of {client.Client.RemoteEndPoint}...");
                    using (FileStream fileHashListStream =
                        new FileStream(fileHashListPath, FileMode.Open, FileAccess.Read))
                    {
                        hashListSerialized = new byte[fileHashListStream.Length];
                        fileHashListStream.Read(hashListSerialized, 0, (int)fileHashListStream.Length);
                    }
                }

                byte[] fileLengthBytes = BitConverter.GetBytes(fileStream.Length);
                NetworkWrite(networkStream, fileLengthBytes, 0, fileLengthBytes.Length);

                byte[] hashListLengthBytes = BitConverter.GetBytes(hashListSerialized.Length);
                NetworkWrite(networkStream, hashListLengthBytes, 0, hashListLengthBytes.Length);

                NetworkWrite(networkStream, hashListSerialized, 0, hashListSerialized.Length);
                Logger?.Log($"Hash list for file {filePath} sent to {client.Client.RemoteEndPoint}.");

                byte[] blockRequestBytes = new byte[sizeof(int)];
                byte[] blockBytes = new byte[preferences.BlockSize];
                byte[] commndBytes = new byte[1];
                while (client.Connected)
                {
                    NetworkRead(networkStream, commndBytes, 0, commndBytes.Length, 60000);
                    byte commandByte = commndBytes[0];
                    Command command = (Command)commandByte;
                    if (command == Command.Terminate)
                    {
                        break;
                    }

                    //networkStream.Read(blockRequestBytes, 0, blockRequestBytes.Length);
                    NetworkRead(networkStream, blockRequestBytes, 0, blockRequestBytes.Length, 60000);

                    int requestStartIndex = BitConverter.ToInt32(blockRequestBytes, 0);
                    NetworkRead(networkStream, blockRequestBytes, 0, blockRequestBytes.Length, 10000);

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
                        FileHashBlock localBlock =
                            FileHashListGenerator.CalculateBlock(blockBytes, 0, blockSize, preferences, i);
                        NetworkWrite(networkStream, blockBytes, 0, blockSize);
                        serverNetStat.Payload += (ulong)(blockRequestBytes.Length);
                    }
                }

                Logger?.Log($"Client {client.Client.RemoteEndPoint} disconnected");
            }
        }

        private void ClientAcceptedMethod(object clientObj)
        {
            TcpClient client = (TcpClient) clientObj;

            Logger?.Log($"Client accepted from {client.Client.RemoteEndPoint}");

            while (client.Connected)
            {
                ClientLoop(client);
            }
        }
    }
}
