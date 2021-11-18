using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands;
using BlockShare.BlockSharing.DirectoryDigesting;
using BlockShare.BlockSharing.HashLists;
using BlockShare.BlockSharing.NetworkStatistics;
using BlockShare.BlockSharing.PreferencesManagement;

namespace BlockShare.BlockSharing
{
    public class BlockShareServer
    {
        private TcpListener tcpListener;

        private Preferences preferences;

        private IPEndPoint localEndpoint;

        private List<X509Certificate> acceptableCertificates = new List<X509Certificate>();

        public ILogger Logger { get; set; }

        private Task worker;

        private bool isServerRunning;

        private NetStat serverNetStat = new NetStat();
        public NetStat GetServerNetStat() => serverNetStat.CloneNetStat();

        private List<TcpClient> activeClients = new List<TcpClient>();

        #region Events
        public event Action<BlockShareServer, IPEndPoint> OnClientConnected;
        public event Action<BlockShareServer, IPEndPoint> OnClientDisconnected;
        public event Action<BlockShareServer, IPEndPoint, string, long, long> OnBlocksRequested;
        public event Action<BlockShareServer, IPEndPoint, string, long> OnBlockUploaded;

        public event Action<BlockShareServer> OnServerStopped;
        public event Action<BlockShareServer, string> OnUnhandledException;

        public event Action<BlockShareServer, string, double> OnHashingProgressChanged;
        public event Action<BlockShareServer, string> OnHashingFinished;

        #endregion

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

        public BlockShareServer(string ip, int port, Preferences preferences, ILogger logger)
        {
            this.preferences = preferences;
            Logger = logger;
            localEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);

            InitializeCertificates();
        }

        public BlockShareServer(Preferences preferences, ILogger logger)
        {
            this.preferences = preferences;
            Logger = logger;
            localEndpoint = new IPEndPoint(IPAddress.Parse(preferences.ServerIp), preferences.ServerPort);

            InitializeCertificates();
        }

        private void InitializeCertificates()
        {
            if (preferences?.SecurityPreferences?.AcceptedCertificatesDirectoryPath != null)
            {
                string path = preferences.SecurityPreferences.AcceptedCertificatesDirectoryPath;
                acceptableCertificates.AddRange(Utils.GetCertificates(path));
            }
        }


        public void StartServer()
        {
            isServerRunning = true;

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
            isServerRunning = false;

            foreach (var client in activeClients)
            {
                client.Close();
            }

            worker.Wait();

            Log($"Server is stopped", 0);
            OnServerStopped?.Invoke(this);
        }

        private void NetworkWrite(Stream stream, byte[] data, int offset, int length)
        {
            stream.Write(data, offset, length);
            serverNetStat.TotalSent += (ulong)length;
        }

        private void NetworkRead(Stream stream, byte[] data, int offset, int length, long timeout)
        {
            Utils.ReadPackage(stream, data, offset, length, timeout);
            serverNetStat.TotalReceived += (ulong)length;
        }

        private void WorkingMethod()
        {
            try
            {
                while (isServerRunning)
                {
                    Log("TcpListener is waiting for next client...", 0);
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Task task = new Task(ClientAcceptedMethod, client);
                    task.Start();
                }
            }
            catch (Exception ex)
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

        private string GetPath(string relativePath)
        {
            string filePath = string.Empty;
            try
            {
                filePath = Path.Combine(preferences.ServerStoragePath, relativePath);
            }
            catch (ArgumentNullException argEx)
            {
                Log("Argument was null: \n" + argEx.Message, 0);
            }
            catch (ArgumentException ex)
            {
                Log("Argument exception occured: \n" + ex.Message, 0);

                // Stupid workaround
                filePath = PathCombineWorkaround(preferences.ServerStoragePath, relativePath);
                Log($"Using workaround for stupid Path.Combine: {filePath}", 0);
            }
            return filePath;
        }

        private void OnHashListGeneratorProgress(Stream fileStream, double progress)
        {
            FileStream fs = (FileStream)fileStream;
            string fileName = fs.Name;
            OnHashingProgressChanged?.Invoke(this, fileName, progress);
        }

        private void OnHashListGeneratorFinished(Stream fileStream)
        {
            FileStream fs = (FileStream)fileStream;
            string fileName = fs.Name;
            OnHashingFinished?.Invoke(this, fileName);
        }


        private ClientLoopResult ClientLoop(TcpClient tcpClient, Stream networkStream)
        {
            //Log($"Waiting for commands from {tcpClient.Client.RemoteEndPoint}...", 1);

            BlockShareCommand command = BlockShareCommand.ReadFromClient(networkStream, serverNetStat, 0);

            switch (command.CommandType)
            {
                case BlockShareCommandType.GetDirectoryDigest:
                    GetDirectoryDigestCommand getDirectoryDigestCommand = (GetDirectoryDigestCommand)command;

                    string digestFilePath = GetPath(getDirectoryDigestCommand.Path);

                    Log($"Client request received: {digestFilePath}", 0);

                    if (!CheckRequestValidity(digestFilePath))
                    {
                        Log($"Client request was invalid: {getDirectoryDigestCommand.Path}", 0);
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareResponseType.InvalidOperation }, 0, 1);
                        InvalidOperationCommand invalidOperationCommand = new InvalidOperationCommand();
                        BlockShareCommand.WriteToClient(invalidOperationCommand, networkStream, serverNetStat);

                        return ClientLoopResult.Disconnect;
                    }

                    if (!Directory.Exists(digestFilePath))
                    {
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareResponseType.InvalidOperation }, 0, 1);
                        InvalidOperationCommand invalidOperationCommand = new InvalidOperationCommand();
                        BlockShareCommand.WriteToClient(invalidOperationCommand, networkStream, serverNetStat);

                        return ClientLoopResult.Continue;
                    }

                    Log($"Generating XML digest with recursion level {getDirectoryDigestCommand.RecursionLevel}...", 0);
                    DirectoryInfo directoryInfo = new DirectoryInfo(digestFilePath);
                    DirectoryInfo rootDirectoryInfo = new DirectoryInfo(preferences.ServerStoragePath);
                    DirectoryDigest directoryDigest = new DirectoryDigest(directoryInfo, rootDirectoryInfo, getDirectoryDigestCommand.RecursionLevel);

                    //byte[] xmlDigestBytes = DirectoryDigest.Serialize(directoryDigest);
                    //int digestLength = xmlDigestBytes.Length;
                    //byte[] digestLengthBytes = BitConverter.GetBytes(digestLength);
                    string xmlDigest = DirectoryDigest.GetXmlString(directoryDigest);

                    SetDirectoryDigestCommand setDirectoryDigestCommand = new SetDirectoryDigestCommand();
                    setDirectoryDigestCommand.XmlPayload = xmlDigest;
                    BlockShareCommand.WriteToClient(setDirectoryDigestCommand, networkStream, serverNetStat);

                    //NetworkWrite(networkStream, digestLengthBytes, 0, digestLengthBytes.Length);

                    //NetworkWrite(networkStream, xmlDigestBytes, 0, xmlDigestBytes.Length);
                    Log($"Digest sent.", 0);
                    return ClientLoopResult.Continue;

                case BlockShareCommandType.GetHashList:
                    GetHashlistCommand getHashlistCommand = (GetHashlistCommand)command;
                    string getHashlistPath = GetPath(getHashlistCommand.Path);
                    if (!CheckRequestValidity(getHashlistPath))
                    {
                        InvalidOperationCommand invalidOperationCommand = new InvalidOperationCommand();
                        BlockShareCommand.WriteToClient(invalidOperationCommand, networkStream, serverNetStat);

                        return ClientLoopResult.Continue;
                    }
                    using (FileStream fileStream = new FileStream(getHashlistPath, FileMode.Open, FileAccess.Read))
                    {
                        //string fileHashListName = fileName + Preferences.HashlistExtension;
                        string fileHashListPath = preferences.HashMapper.GetHashlistFile(getHashlistPath);
                        //Log($"Hashlist file name: {fileHashListName}", 2);

                        byte[] hashListSerialized = null;

                        Log($"On-server file size: {fileStream.Length}", 2);

                        //string fileHashListPath = Path.Combine(preferences.ServerStoragePath, fileHashListName);
                        FileHashList hashList;
                        if (!File.Exists(fileHashListPath))
                        {
                            Log(
                                $"Calculating hash list for file {getHashlistPath}...", 2);
                            hashList = FileHashListGenerator.GenerateHashList(fileStream, null, preferences,
                                OnHashListGeneratorProgress, OnHashListGeneratorFinished);
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
                                $"Reading hash list of file {getHashlistPath}...", 2);
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
                        BlockShareCommand.WriteToClient(setHashlistCommand, networkStream, serverNetStat);

                        Log($"Hash list for file {getHashlistPath} sent", 1);
                    }
                    return ClientLoopResult.Continue;

                case BlockShareCommandType.GetBlockRange:
                    //string getBlockRangeRelativePath = ReadString(tcpClient, 10000);
                    //byte[] blockRequestBytes = new byte[sizeof(int)];
                    byte[] blockBytes = new byte[preferences.BlockSize];
                    GetBlockRangeCommand getBlockRangeCommand = (GetBlockRangeCommand)command;
                    string getBlocksFilePath = GetPath(getBlockRangeCommand.Path);

                    IPEndPoint clientEp = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                    OnBlocksRequested?.Invoke(this, clientEp, getBlocksFilePath, getBlockRangeCommand.BlockIndex, getBlockRangeCommand.BlocksCount);

                    if (!CheckRequestValidity(getBlocksFilePath))
                    {
                        // FIXME Shitty method to deal with such request.
                        return ClientLoopResult.Disconnect;
                    }

                    using (FileStream fileStream = new FileStream(getBlocksFilePath, FileMode.Open, FileAccess.Read))
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

                            OnBlockUploaded?.Invoke(this, clientEp, getBlocksFilePath, i);
                        }
                    }

                    return ClientLoopResult.Continue;

                case BlockShareCommandType.Disconnect:
                    return ClientLoopResult.Disconnect;

                case BlockShareCommandType.GetEntryType:
                    //string getEntryTypeRelativePath = ReadString(tcpClient, 10000);
                    GetEntryTypeCommand getEntryTypeCommand = (GetEntryTypeCommand)command;
                    string getEntryTypePath = GetPath(getEntryTypeCommand.Path);
                    if (!CheckRequestValidity(getEntryTypePath))
                    {
                        Log($"Client request was invalid: {getEntryTypeCommand.Path}", 0);
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareCommandType.Ok, (byte)FileSystemEntryType.NonExistent }, 0, 2);
                        SetEntryTypeCommand setEntryTypeCommand = new SetEntryTypeCommand(FileSystemEntryType.NonExistent);
                        BlockShareCommand.WriteToClient(setEntryTypeCommand, networkStream, serverNetStat);
                    }
                    else if (Directory.Exists(getEntryTypePath))
                    {
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareCommandType.Ok, (byte)FileSystemEntryType.Directory }, 0, 2);
                        SetEntryTypeCommand setEntryTypeCommand = new SetEntryTypeCommand(FileSystemEntryType.Directory);
                        BlockShareCommand.WriteToClient(setEntryTypeCommand, networkStream, serverNetStat);
                    }
                    else if (File.Exists(getEntryTypePath))
                    {
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareCommandType.Ok, (byte)FileSystemEntryType.File }, 0, 2);
                        SetEntryTypeCommand setEntryTypeCommand = new SetEntryTypeCommand(FileSystemEntryType.File);
                        BlockShareCommand.WriteToClient(setEntryTypeCommand, networkStream, serverNetStat);
                    }
                    else
                    {
                        //NetworkWrite(networkStream, new byte[] { (byte)BlockShareCommandType.Ok, (byte)FileSystemEntryType.NonExistent }, 0, 2);
                        SetEntryTypeCommand setEntryTypeCommand = new SetEntryTypeCommand(FileSystemEntryType.NonExistent);
                        BlockShareCommand.WriteToClient(setEntryTypeCommand, networkStream, serverNetStat);
                    }
                    return ClientLoopResult.Continue;

                case BlockShareCommandType.GetFileDigest:
                    GetFileDigestCommand getFileInfoCommand = (GetFileDigestCommand)command;
                    string path = GetPath(getFileInfoCommand.Path);
                    bool invalidRequest = false;
                    if (!CheckRequestValidity(path))
                    {
                        invalidRequest = true;
                    }
                    else if (!File.Exists(path))
                    {
                        invalidRequest = true;
                    }
                    if (invalidRequest)
                    {
                        InvalidOperationCommand invalidOperationCommand = new InvalidOperationCommand();
                        BlockShareCommand.WriteToClient(invalidOperationCommand, networkStream, serverNetStat);
                        return ClientLoopResult.Continue;
                    }

                    SetFileDigestCommand setFileDigestCommand = new SetFileDigestCommand();

                    //FileInfo fileInfo = new FileInfo(path);
                    FileDigest fileDigest = new FileDigest(getFileInfoCommand.Path, path);
                    setFileDigestCommand.FileDigest = fileDigest;

                    BlockShareCommand.WriteToClient(setFileDigestCommand, networkStream, serverNetStat);

                    return ClientLoopResult.Continue;
            }

            return ClientLoopResult.Disconnect;
        }

        private bool ValidateClientCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
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

        private void LogSecurityInfo(SslStream stream)
        {
            Log(String.Format("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength), 0);
            Log(String.Format("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength), 0);
            Log(String.Format("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength), 0);
            Log(String.Format("Protocol: {0}", stream.SslProtocol), 0);

            Log(String.Format("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer), 0);
            Log(String.Format("IsSigned: {0}", stream.IsSigned), 0);
            Log(String.Format("Is Encrypted: {0}", stream.IsEncrypted), 0);

            Log(String.Format("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite), 0);
            Log(String.Format("Can timeout: {0}", stream.CanTimeout), 0);

            Log(String.Format("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus), 0);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Log(String.Format("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString()), 0);
            }
            else
            {
                Log(String.Format("Local certificate is null."), 0);
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Log(String.Format("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString()), 0);
            }
            else
            {
                Log(String.Format("Remote certificate is null."), 0);
            }

        }

        private void ClientAcceptedMethod(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            IPEndPoint clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;

            using (client)
            {

                Stream networkStream = null;
                if (preferences.SecurityPreferences != null && preferences.SecurityPreferences.Method != SecurityMethod.None)
                {
                    Log($"Using security method: {preferences.SecurityPreferences.Method}", 0);
                    X509Certificate2 serverCertificate = null;
                    try
                    {
                        serverCertificate = Utils.CreateFromPkcs12(preferences.SecurityPreferences.ServerCertificatePath);
                    }
                    catch(FileNotFoundException ex)
                    {
                        Log($"Server Certificate: {ex.Message}", 0);
                        client.Close();
                        return;
                    }
                    catch(CryptographicException ex)
                    {
                        Log($"Failed to load Server Certificate: {ex.Message}", 0);
                        client.Close();
                        return;
                    }

                    SslStream sslStream = new SslStream(
                        client.GetStream(),
                        false,
                        new RemoteCertificateValidationCallback(ValidateClientCertificate),
                        null,
                        EncryptionPolicy.RequireEncryption
                        );

                    try
                    {
                        sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: true, checkCertificateRevocation: true);
                        LogSecurityInfo(sslStream);

                        //sslStream.ReadTimeout = 60000;
                        //sslStream.WriteTimeout = 60000;

                        networkStream = sslStream;
                    }
                    catch (AuthenticationException e)
                    {
                        Log($"Exception: {e.Message}", 0);
                        if (e.InnerException != null)
                        {
                            Log($"Inner exception: {e.InnerException.Message}", 0);
                        }
                        Log("Authentication failed - closing the connection.", 0);
                        sslStream.Close();
                        client.Close();
                        return;
                    }

                }
                else
                {
#if !ENSURE_SECURITY
                    Log($"No security method used", 0);
                    networkStream = client.GetStream();
#else
                    Log($"Server was built with ENSURE_SECURITY. Enable any security method.", 0);
                    networkStream = null;

                    Log($"Client disconnected", 0);
                    lock (activeClients)
                    {
                        activeClients.Remove(client);
                    }
                    client.Close();
                    return;
#endif
                }

                Log($"Client accepted from {clientEndPoint}", 0);
                lock (activeClients)
                {
                    activeClients.Add(client);
                }
                OnClientConnected?.Invoke(this, clientEndPoint);
                try
                {
                    while (client.Connected)
                    {
                        //ClientLoopOld(client);
                        ClientLoopResult loopResult = ClientLoop(client, networkStream);
                        switch (loopResult)
                        {
                            case ClientLoopResult.Continue:
                                continue;
                            case ClientLoopResult.Disconnect:
                                break;
                        }
                    }
                }
                catch (TimeoutException ex)
                {
                    Log("Client was timed out: \n" + ex.Message, 0);
#if DEBUG
                    Log(ex.StackTrace, 0);
#endif
                }
                catch (IOException)
                {
                    Log("Unexpected IO exception. Client disconnected without invoking Disconnect?", 0);
                }
                catch (Exception ex)
                {
                    OnUnhandledException?.Invoke(this, ex.Message);
                    Log(ex.Message, 0);
#if DEBUG
                    Log(ex.StackTrace, 0);
                    throw ex;
#endif
                }

                Log($"Client disconnected", 0);
                lock (activeClients)
                {
                    activeClients.Remove(client);
                }
                OnClientDisconnected?.Invoke(this, clientEndPoint);
                client.Close();
            }
        }
    }
}
