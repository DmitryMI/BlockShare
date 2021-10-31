using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands;
using BlockShare.BlockSharing.DirectoryDigesting;

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
        public NetStat GetServerNetStat() => serverNetStat.CloneNetStat();

        private List<IPEndPoint> activeClients = new List<IPEndPoint>();

        #region Events
        public event Action<BlockShareServer, IPEndPoint> OnClientConnected;
        public event Action<BlockShareServer, IPEndPoint> OnClientDisconnected;
        public event Action<BlockShareServer> OnServerStopped;
        public event Action<BlockShareServer, string> OnUnhandledException;
        #endregion

        public IPEndPoint[] GetActiveClients()
        {
            lock(activeClients)
            {
                return activeClients.ToArray();
            }
        }

        private void Log(string message, int withVerbosity)
        {
            if (withVerbosity <= preferences.Verbosity)
            {
                Logger?.Log(message);
            }
        }

        public string GetLocalEndpoint()
        {
            if (tcpListener == null)
            {
                return string.Empty;
            }

            return tcpListener.Server.LocalEndPoint.ToString();
        }

        public BlockShareServer(string ip, int port, Preferences preferences, IProgressReporter hashProgress, ILogger logger)
        {
            HashListGeneratorReporter = hashProgress;
            this.preferences = preferences;
            Logger = logger;
            localEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public BlockShareServer(Preferences preferences, IProgressReporter hashProgress, ILogger logger)
        {
            HashListGeneratorReporter = hashProgress;
            this.preferences = preferences;
            Logger = logger;
            localEndpoint = new IPEndPoint(IPAddress.Parse(preferences.ServerIp), preferences.ServerPort);
        }

        public void StartServer()
        {
            Log($"Starting server on {localEndpoint}...", 0);
            tcpListener = new TcpListener(localEndpoint);
            tcpListener.Start();

            worker = new Task(WorkingMethod);
            worker.Start();

            Log($"Server is now listening on {localEndpoint}", 0);
        }

        public void StopServer()
        {
            tcpListener.Stop();
            worker.Wait();

            Log($"Server is stopped", 0);
            OnServerStopped?.Invoke(this);
        }

        private void NetworkWrite(NetworkStream stream, byte[] data, int offset, int length)
        {
            stream.Write(data, offset, length);
            serverNetStat.TotalSent += (ulong) length;
        }

        private void NetworkRead(TcpClient tcpClient, NetworkStream stream, byte[] data, int offset, int length, long timeout)
        {
            Utils.ReadPackage(tcpClient, stream, data, offset, length, timeout);
            serverNetStat.TotalReceived += (ulong) length;
        }

        private void WorkingMethod()
        {
            try
            {
                while (true)
                {
                    Log("TcpListener is waiting for next client...", 0);
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Task task = new Task(ClientAcceptedMethod, client);
                    task.Start();
                }
            }
            catch(Exception ex)
            {
                Log(ex.Message, 0);
            }
        }
        
        private bool CheckRequestValidity(string requestedEntry)
        {
            if (String.IsNullOrWhiteSpace(requestedEntry))
            {
                Log("Requested Entry is empty", 0);
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
                Log($"Error: file or directory {requestedEntry} does not exist", 0);
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
                Log($"Security error: {entryInfo.FullName} is not inside Server Folder", 0);
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

        public string ReadString(TcpClient tcpClient, long timeout)
        {
            NetworkStream networkStream = tcpClient.GetStream();

            byte[] valueLengthBytes = new byte[sizeof(int)];

            NetworkRead(tcpClient, networkStream, valueLengthBytes, 0, sizeof(int), timeout);
            int valueLength = BitConverter.ToInt32(valueLengthBytes, 0);
            byte[] fileNameBytes = new byte[valueLength];
            NetworkRead(tcpClient, networkStream, fileNameBytes, 0, valueLength, timeout);

            string value = Encoding.UTF8.GetString(fileNameBytes);
            return value;
        }

        public void WriteString(TcpClient tcpClient, string value)
        {

        }

        private ClientLoopResult ClientLoop(TcpClient tcpClient)
        {
            NetworkStream networkStream = tcpClient.GetStream();

            Log($"Waiting for commands from {tcpClient.Client.RemoteEndPoint}...", 1);

            BlockShareCommand command = BlockShareCommand.ReadFromClient(tcpClient, serverNetStat, 60000);

            switch (command.CommandType)
            {
                case BlockShareCommandType.GetDirectoryDigest:
                    GetDirectoryDigestCommand getDirectoryDigestCommand = (GetDirectoryDigestCommand)command;
                    if (!CheckRequestValidity(getDirectoryDigestCommand.Path))
                    {
                        Log($"Client request was invalid: {getDirectoryDigestCommand.Path}", 0);
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareResponseType.InvalidOperation }, 0, 1);
                        InvalidOperationCommand invalidOperationCommand = new InvalidOperationCommand();
                        BlockShareCommand.WriteToClient(invalidOperationCommand, tcpClient, serverNetStat);

                        return ClientLoopResult.Disconnect;
                    }

                    if (!Directory.Exists(getDirectoryDigestCommand.Path))
                    {
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareResponseType.InvalidOperation }, 0, 1);
                        InvalidOperationCommand invalidOperationCommand = new InvalidOperationCommand();
                        BlockShareCommand.WriteToClient(invalidOperationCommand, tcpClient, serverNetStat);

                        return ClientLoopResult.Continue;
                    }

                    Log($"Generating XML digest with recursion level {getDirectoryDigestCommand.RecursionLevel}...", 0);
                    DirectoryInfo directoryInfo = new DirectoryInfo(getDirectoryDigestCommand.Path);
                    DirectoryInfo rootDirectoryInfo = new DirectoryInfo(preferences.ServerStoragePath);
                    DirectoryDigest directoryDigest = new DirectoryDigest(directoryInfo, rootDirectoryInfo, getDirectoryDigestCommand.RecursionLevel);

                    //byte[] xmlDigestBytes = DirectoryDigest.Serialize(directoryDigest);
                    //int digestLength = xmlDigestBytes.Length;
                    //byte[] digestLengthBytes = BitConverter.GetBytes(digestLength);
                    string xmlDigest = DirectoryDigest.GetXmlString(directoryDigest);

                    SetDirectoryDigestCommand setDirectoryDigestCommand = new SetDirectoryDigestCommand();
                    setDirectoryDigestCommand.XmlPayload = xmlDigest;
                    BlockShareCommand.WriteToClient(setDirectoryDigestCommand, tcpClient, serverNetStat);

                    //NetworkWrite(networkStream, digestLengthBytes, 0, digestLengthBytes.Length);

                    //NetworkWrite(networkStream, xmlDigestBytes, 0, xmlDigestBytes.Length);
                    Log($"Digest sent.", 0);
                    return ClientLoopResult.Continue;

                case BlockShareCommandType.GetHashList:
                    GetHashlistCommand getHashlistCommand = (GetHashlistCommand)command;
                    if (!CheckRequestValidity(getHashlistCommand.Path))
                    {
                        InvalidOperationCommand invalidOperationCommand = new InvalidOperationCommand();
                        BlockShareCommand.WriteToClient(invalidOperationCommand, tcpClient, serverNetStat);

                        return ClientLoopResult.Continue;
                    }
                    using (FileStream fileStream = new FileStream(getHashlistCommand.Path, FileMode.Open, FileAccess.Read))
                    {
                        //string fileHashListName = fileName + Preferences.HashlistExtension;
                        string fileHashListPath = preferences.HashMapper.GetHashlistFile(getHashlistCommand.Path);
                        //Log($"Hashlist file name: {fileHashListName}", 2);

                        byte[] hashListSerialized = null;

                        Log($"On-server file size: {fileStream.Length}", 2);

                        //string fileHashListPath = Path.Combine(preferences.ServerStoragePath, fileHashListName);
                        FileHashList hashList;
                        if (!File.Exists(fileHashListPath))
                        {
                            Log(
                                $"Calculating hash list for file {getHashlistCommand.Path} by request of {tcpClient.Client.RemoteEndPoint}...", 2);
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
                            Log(
                                $"Reading hash list of file {getHashlistCommand.Path} by request of {tcpClient.Client.RemoteEndPoint}...", 2);
                            using (FileStream fileHashListStream =
                                new FileStream(fileHashListPath, FileMode.Open, FileAccess.Read))
                            {
                                hashListSerialized = new byte[fileHashListStream.Length];
                                fileHashListStream.Read(hashListSerialized, 0, (int)fileHashListStream.Length);
                            }
                        }

                        SetHashlistCommand setHashlistCommand = new SetHashlistCommand();
                        setHashlistCommand.FileLength = fileStream.Length;
                        setHashlistCommand.HashlistSerialized = hashListSerialized;
                        BlockShareCommand.WriteToClient(setHashlistCommand, tcpClient, serverNetStat);

                        Log($"Hash list for file {getHashlistCommand.Path} sent to {tcpClient.Client.RemoteEndPoint}.", 1);
                    }
                    return ClientLoopResult.Continue;

                case BlockShareCommandType.GetBlockRange:
                    //string getBlockRangeRelativePath = ReadString(tcpClient, 10000);
                    //byte[] blockRequestBytes = new byte[sizeof(int)];
                    byte[] blockBytes = new byte[preferences.BlockSize];
                    GetBlockRangeCommand getBlockRangeCommand = (GetBlockRangeCommand)command;
                    
                    using (FileStream fileStream = new FileStream(getBlockRangeCommand.Path, FileMode.Open, FileAccess.Read))
                    {
                        long requestStartIndex = getBlockRangeCommand.BlockIndex;
                        long requestBlocksNumber = getBlockRangeCommand.BlocksCount;
                        for (long i = requestStartIndex; i < requestStartIndex + requestBlocksNumber; i++)
                        {
                            long filePosition = (i * preferences.BlockSize);
                            fileStream.Seek(filePosition, SeekOrigin.Begin);
                            long blockSize;
                            long bytesLeft = fileStream.Length - filePosition;
                            if (bytesLeft > preferences.BlockSize)
                            {
                                blockSize = preferences.BlockSize;
                            }
                            else
                            {
                                blockSize = bytesLeft;
                            }

                            fileStream.Read(blockBytes, 0, (int)blockSize);
                            NetworkWrite(networkStream, blockBytes, 0, (int)blockSize);
                            serverNetStat.Payload += (ulong)(blockSize);
                        }
                    }

                    return ClientLoopResult.Continue;

                case BlockShareCommandType.Disconnect:
                    return ClientLoopResult.Disconnect;

                case BlockShareCommandType.GetEntryType:
                    //string getEntryTypeRelativePath = ReadString(tcpClient, 10000);
                    GetEntryTypeCommand getEntryTypeCommand = (GetEntryTypeCommand)command;
                    if (!CheckRequestValidity(getEntryTypeCommand.Path))
                    {
                        Log($"Client request was invalid: {getEntryTypeCommand.Path}", 0);
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareCommandType.Ok, (byte)FileSystemEntryType.NonExistent }, 0, 2);
                        SetEntryTypeCommand setEntryTypeCommand = new SetEntryTypeCommand(FileSystemEntryType.NonExistent);
                        BlockShareCommand.WriteToClient(setEntryTypeCommand, tcpClient, serverNetStat);
                    }
                    else if (Directory.Exists(getEntryTypeCommand.Path))
                    {
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareCommandType.Ok, (byte)FileSystemEntryType.Directory }, 0, 2);
                        SetEntryTypeCommand setEntryTypeCommand = new SetEntryTypeCommand(FileSystemEntryType.Directory);
                        BlockShareCommand.WriteToClient(setEntryTypeCommand, tcpClient, serverNetStat);
                    }
                    else if (File.Exists(getEntryTypeCommand.Path))
                    {
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareCommandType.Ok, (byte)FileSystemEntryType.File }, 0, 2);
                        SetEntryTypeCommand setEntryTypeCommand = new SetEntryTypeCommand(FileSystemEntryType.File);
                        BlockShareCommand.WriteToClient(setEntryTypeCommand, tcpClient, serverNetStat);
                    }
                    else
                    {
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareCommandType.Ok, (byte)FileSystemEntryType.NonExistent }, 0, 2);
                        SetEntryTypeCommand setEntryTypeCommand = new SetEntryTypeCommand(FileSystemEntryType.NonExistent);
                        BlockShareCommand.WriteToClient(setEntryTypeCommand, tcpClient, serverNetStat);
                    }
                    return ClientLoopResult.Continue;
            }

            return ClientLoopResult.Disconnect;
        }

        [Obsolete]
        private void ClientLoopOld(TcpClient tcpClient)
        {
            NetworkStream networkStream = tcpClient.GetStream();

            Log($"Waiting for file request from {tcpClient.Client.RemoteEndPoint}...", 1);

            byte[] fileNameLengthBytes = new byte[sizeof(int)];

            NetworkRead(tcpClient, networkStream, fileNameLengthBytes, 0, sizeof(int), 10000);

            int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);
            byte[] fileNameBytes = new byte[fileNameLength];

            NetworkRead(tcpClient, networkStream, fileNameBytes, 0, fileNameLength, 10000);

            string fileName = Encoding.UTF8.GetString(fileNameBytes);
            string filePath = string.Empty;            

            try
            {
                filePath = Path.Combine(preferences.ServerStoragePath, fileName);
            }
            catch (ArgumentNullException argEx)
            {
                Log("Argument was null: \n" + argEx.Message, 0);
            }
            catch (ArgumentException ex)
            {
                Log("Argument exception occured: \n" + ex.Message, 0);

                // Stupid workaround
                filePath = PathCombineWorkaround(preferences.ServerStoragePath, fileName);
                Log($"Using workaround for stupid Path.Combine: {filePath}", 0);
            }

            Log($"Client request received: {fileName}", 0);

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
                entryTypeMessage = new byte[] { (byte)FileSystemEntryType.Directory };
                NetworkWrite(networkStream, entryTypeMessage, 0, entryTypeMessage.Length);

                byte[] recursionLevelBytes = new byte[sizeof(int)];
                NetworkRead(tcpClient, networkStream, recursionLevelBytes, 0, recursionLevelBytes.Length, 10000);
                int recursionLevel = BitConverter.ToInt32(recursionLevelBytes, 0);
                Log($"Directory was requested. Generating XML digest with recursion level {recursionLevel}...", 0);
                DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
                DirectoryInfo rootDirectoryInfo = new DirectoryInfo(preferences.ServerStoragePath);
                DirectoryDigest directoryDigest = new DirectoryDigest(directoryInfo, rootDirectoryInfo, recursionLevel);
                                
                byte[] xmlDigestBytes = DirectoryDigest.Serialize(directoryDigest);
                int digestLength = xmlDigestBytes.Length;
                byte[] digestLengthBytes = BitConverter.GetBytes(digestLength);                

                NetworkWrite(networkStream, digestLengthBytes, 0, digestLengthBytes.Length);

                NetworkWrite(networkStream, xmlDigestBytes, 0, xmlDigestBytes.Length);
                Log($"Digest sent.", 0);
                return;
            }

            entryTypeMessage = new byte[] { (byte)FileSystemEntryType.File };
            NetworkWrite(networkStream, entryTypeMessage, 0, entryTypeMessage.Length);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                //string fileHashListName = fileName + Preferences.HashlistExtension;
                string fileHashListPath = preferences.HashMapper.GetHashlistFile(filePath);
                //Log($"Hashlist file name: {fileHashListName}", 2);

                byte[] hashListSerialized = null;

                Log($"On-server file size: {fileStream.Length}", 2);

                //string fileHashListPath = Path.Combine(preferences.ServerStoragePath, fileHashListName);
                FileHashList hashList;
                if (!File.Exists(fileHashListPath))
                {
                    Log(
                        $"Calculating hash list for file {filePath} by request of {tcpClient.Client.RemoteEndPoint}...", 2);
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
                    Log(
                        $"Reading hash list of file {filePath} by request of {tcpClient.Client.RemoteEndPoint}...", 2);
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
                Log($"Hash list for file {filePath} sent to {tcpClient.Client.RemoteEndPoint}.", 1);

                byte[] blockRequestBytes = new byte[sizeof(int)];
                byte[] blockBytes = new byte[preferences.BlockSize];
                byte[] commndBytes = new byte[1];
                while (tcpClient.Connected)
                {
                    NetworkRead(tcpClient, networkStream, commndBytes, 0, commndBytes.Length, 120000);
                    byte commandByte = commndBytes[0];
                    Command command = (Command)commandByte;
                    if (command == Command.Terminate)
                    {
                        Log("Transmission terminated by client request", 0);
                        break;
                    }

                    //networkStream.Read(blockRequestBytes, 0, blockRequestBytes.Length);
                    NetworkRead(tcpClient, networkStream, blockRequestBytes, 0, blockRequestBytes.Length, 10000);

                    long requestStartIndex = BitConverter.ToInt32(blockRequestBytes, 0);
                    NetworkRead(tcpClient, networkStream, blockRequestBytes, 0, blockRequestBytes.Length, 10000);

                    long requestBlocksNumber = BitConverter.ToInt32(blockRequestBytes, 0);
                    for (long i = requestStartIndex; i < requestStartIndex + requestBlocksNumber; i++)
                    {
                        long filePosition = (i * preferences.BlockSize);
                        fileStream.Seek(filePosition, SeekOrigin.Begin);
                        long blockSize;
                        long bytesLeft = fileStream.Length - filePosition;
                        if (bytesLeft > preferences.BlockSize)
                        {
                            blockSize = preferences.BlockSize;
                        }
                        else
                        {
                            blockSize = bytesLeft;
                        }

                        fileStream.Read(blockBytes, 0, (int)blockSize);
                        //FileHashBlock localBlock =
                        //FileHashListGenerator.CalculateBlock(blockBytes, 0, blockSize, preferences, i);
                        NetworkWrite(networkStream, blockBytes, 0, (int)blockSize);
                        serverNetStat.Payload += (ulong)(blockRequestBytes.Length);
                    }
                }
            }
        }

        private void ClientAcceptedMethod(object clientObj)
        {
            TcpClient client = (TcpClient) clientObj;
            using (client)
            {
                Log($"Client accepted from {client.Client.RemoteEndPoint}", 0);
                lock(activeClients)
                {
                    activeClients.Add((IPEndPoint)client.Client.RemoteEndPoint);                    
                }
                OnClientConnected?.Invoke(this, (IPEndPoint)client.Client.RemoteEndPoint);
                try
                {
                    while (client.Connected)
                    {
                        //ClientLoopOld(client);
                        ClientLoopResult loopResult = ClientLoop(client);
                        switch (loopResult)
                        {
                            case ClientLoopResult.Continue:
                                break;
                            case ClientLoopResult.Disconnect:
                                client.Close();                                
                                break;
                        }
                    }
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine("Client was timed out: \n" + ex.Message);
#if DEBUG
                    Console.WriteLine(ex.StackTrace);
#endif
                }
                catch (Exception ex)
                {
                    OnUnhandledException?.Invoke(this, ex.Message);
                    Console.WriteLine(ex.Message);
#if DEBUG
                    Console.WriteLine(ex.StackTrace);
                    throw ex;
#endif
                }

                Log($"Client {client.Client.RemoteEndPoint} disconnected", 0);
                lock (activeClients)
                {
                    activeClients.Remove((IPEndPoint)client.Client.RemoteEndPoint);                    
                }
                OnClientDisconnected?.Invoke(this, (IPEndPoint)client.Client.RemoteEndPoint);
            }
        }
    }
}
