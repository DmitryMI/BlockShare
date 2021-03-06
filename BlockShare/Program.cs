using BlockShare.BlockSharing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockShare.BlockSharing.HashMapping;
using BlockShare.BlockSharing.DirectoryDigesting;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using BlockShare.BlockSharing.Gui;
using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.PreferencesManagement;
using BlockShare.BlockSharing.NetworkStatistics;
using BlockShare.BlockSharing.HashLists;
using System.Reflection;
using BlockShare.BlockSharing.PreferencesManagement.Exceptions;
using BlockShare.BlockSharing.StorageMapping;

namespace BlockShare
{
    class Program
    {
        private class ConsoleLogger : ILogger
        {
            private Dictionary<string, double> hashingProgressTable = new Dictionary<string, double>();
            private Dictionary<string, double> downloadingProgressTable = new Dictionary<string, double>();

            public string Prefix { get; set; }
            
            public ConsoleLogger(string prefix)
            {
                Prefix = prefix;
            }
            public void Log(string message)
            {
                Console.WriteLine(Prefix + " " + message);
            }

            public void OnHashingProgressChanged(string file, double progress)
            {
                if(!hashingProgressTable.ContainsKey(file))
                {
                    hashingProgressTable.Add(file, 0);                    
                }

                double prevProgress = hashingProgressTable[file];
                if (progress - prevProgress > 0.01f)
                {
                    Console.WriteLine(Prefix + $" Hashing {file}: {progress * 100.0:F1}%");
                    hashingProgressTable[file] = progress;
                }                
            }

            public void OnBlockDownloaded(DownloadingProgressEventData eventData)
            {
                double previousProgress = 0;
                double progress;
                if (eventData.RemoteHashList == null || eventData.LocalHashList == null)
                {
                    progress = (double)eventData.DownloadedBlockIndex / eventData.BlocksCount;
                }
                else
                {
                    int downloadedBlocksCount = 0;
                    for(int i = 0; i < eventData.LocalHashList.BlocksCount && i < eventData.RemoteHashList.BlocksCount; i++)
                    {
                        if(eventData.LocalHashList[i] == eventData.RemoteHashList[i])
                        {
                            downloadedBlocksCount++;
                        }
                    }
                    progress = (double)downloadedBlocksCount / eventData.RemoteHashList.BlocksCount;
                }

                if (!downloadingProgressTable.ContainsKey(eventData.FileName))
                {
                    downloadingProgressTable.Add(eventData.FileName, progress);                    
                }
                else
                {
                    previousProgress = downloadingProgressTable[eventData.FileName];
                }

                if (progress - previousProgress >= 0.01f)
                {
                    Console.WriteLine(Prefix + $" Downloading {eventData.FileName}: {progress * 100.0:F1}%");
                    downloadingProgressTable[eventData.FileName] = progress;
                }                
            }

            public void OnHashingFinished(string file)
            {
                Console.WriteLine(Prefix + $" Hashing {file}: finished");
                hashingProgressTable.Remove(file);
            }

            public void OnDownloadingFinished(string file)
            {
                Console.WriteLine(Prefix + $" Downloading {file}: finished");
                downloadingProgressTable.Remove(file);
            }
        }        

        

        static void Download(BlockShareClient client, Preferences preferences, ILogger clientLogger, string fileName)
        {
            if (fileName[0] == Path.DirectorySeparatorChar || fileName[0] == Path.AltDirectorySeparatorChar)
            {
                fileName = fileName.Remove(0, 1);
            }

            client.ClearNetStat();
            Stopwatch sw = Stopwatch.StartNew();            
            client.DownloadFile(fileName);
            sw.Stop();
            long millis = sw.ElapsedMilliseconds;
            if (File.Exists(fileName))
            {
                Console.WriteLine($"File {fileName} was downloaded");
            }
            else if (Directory.Exists(fileName))
            {
                Console.WriteLine($"Directory {fileName} was downloaded");
            }
            else
            {
                Console.WriteLine("Nothing was downloaded");
            }

            NetStat clientNetStat = client.CloneNetStat();

            if (clientNetStat.TotalReceived == 0)
            {
                Console.WriteLine("No useful data was received during this session");
            }
            else
            {
                double efficiency = (double)clientNetStat.Payload /
                                    (clientNetStat.TotalSent + clientNetStat.TotalReceived);
                double downloadingSpeed = (double)clientNetStat.TotalReceived / (millis / 1000.0f);
                double efficiencyPercent = efficiency * 100.0f;
                double downloadingSpeedMibS = downloadingSpeed / 1024 / 1024;
                Console.WriteLine($"Efficiency: {efficiencyPercent}%");
                Console.WriteLine($"Speed: {downloadingSpeedMibS} MiB/s");
            }
        }

        static void PrehashFile(string filePath, Preferences preferences)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                //string fileHashListName = fileName + Preferences.HashlistExtension;
                string fileHashListPath = preferences.HashMapper.GetHashlistFile(filePath);
                //Log($"Hashlist file name: {fileHashListName}", 2);

                byte[] hashListSerialized = null;

                Console.WriteLine($"Hashing {filePath}...");

                //string fileHashListPath = Path.Combine(preferences.ServerStoragePath, fileHashListName);
                FileHashList hashList;
                if (!File.Exists(fileHashListPath))
                {
                    hashList = FileHashListGenerator.GenerateHashList(fileStream, null, preferences,
                        null);
                    hashListSerialized = hashList.Serialize();
                    using (FileStream fileHashListStream =
                        new FileStream(fileHashListPath, FileMode.CreateNew, FileAccess.Write))
                    {
                        fileHashListStream.Write(hashListSerialized, 0, hashListSerialized.Length);
                    }
                }
                else
                {
                    using (FileStream fileHashListStream =
                        new FileStream(fileHashListPath, FileMode.Open, FileAccess.Read))
                    {
                        hashListSerialized = new byte[fileHashListStream.Length];
                        fileHashListStream.Read(hashListSerialized, 0, (int) fileHashListStream.Length);
                    }
                }
            }
        }

        static void Prehash(Preferences preferences)
        {
            Console.WriteLine($"Prehashing: {preferences.ServerStoragePath}");

            if (File.Exists(preferences.ServerStoragePath))
            {
                PrehashFile(preferences.ServerStoragePath, preferences);
            }
            else if (Directory.Exists(preferences.ServerStoragePath))
            {
                Utils.ForEachFsEntry<Preferences>(preferences.ServerStoragePath, preferences, PrehashFile);
            }
            else
            {
                Console.WriteLine($"File or Directory {preferences.ServerStoragePath} does not exist");
            }

            Console.WriteLine("Prehashing finished");
        }

        class BrowserInput
        {
            public string Command { get; set; }
            public int[] EntryIndexes { get; set; } 

            public BrowserInput(string consoleInput)
            {
                string[] words = consoleInput.Split(' ');
                bool isFirstWordIndex = int.TryParse(words[0], out int index);
                if(isFirstWordIndex)
                {
                    Command = "E";
                    EntryIndexes = new int[] { index };
                }
                else
                {
                    Command = words[0];
                    List<int> indexes = new List<int>();
                    for(int i = 1; i < words.Length; i++)
                    {
                        if(words[i] == "-")
                        {
                            bool nextIndexCorrect = int.TryParse(words[i + 1], out int nextIndex);                            
                            int lastIndex = indexes[indexes.Count - 1];
                            int[] range = Utils.Range(lastIndex + 1, nextIndex);
                            indexes.AddRange(range);
                            i++;
                        }
                        else
                        {
                            int nextIndex = int.Parse(words[i]);
                            indexes.Add(nextIndex);
                        }
                    }
                    EntryIndexes = indexes.ToArray();
                }
            }
        }

        static void Browser(BlockShareClient client, Preferences preferences, ILogger clientLogger, string fileName)
        {
            StorageMapper storageMapper = new StorageMapper(preferences, preferences.StorageMappingFile, clientLogger);

            DirectoryDigest rootDigest = client.GetDirectoryDigest(fileName, preferences.BrowserRecursionLevel);
            if (rootDigest == null)
            {
                Console.WriteLine("Error during receiving remote file system info");
                return;
            }
            
            DirectoryDigest current = rootDigest;
            Stack<DirectoryDigest> pathStack = new Stack<DirectoryDigest>();
            while (true)
            {
                if (!current.IsLoaded)
                {
                    Console.WriteLine("Loading...");
                    //DirectoryDigest digest = client.GetDirectoryDigest(ip, port, current.RelativePath, 1 );
                    DirectoryDigest digest = client.GetDirectoryDigest(current.RelativePath, preferences.BrowserRecursionLevel);
                    current.LoadEntriesFrom(digest);
                }

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Current directory: {current.RelativePath}\n");                
                
                Console.WriteLine("Directories: \n");
                var dirs = current.GetSubDirectories();
                if (dirs.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("NO DIRECTORIES");
                }
                for (int i = 0; i < dirs.Count; i++)
                {
                    // Index
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{i}. ");
                    // Name
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(dirs[i].Name);
                    Console.Write($" ");
                    // Size
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    string sizeFormat = Utils.FormatByteSize(dirs[i].Size);
                    Console.WriteLine($"({sizeFormat})");
                    // TODO Directory Percentage
                    
                }

                Console.WriteLine("\nFiles: \n");
                var files = current.GetFiles();
                if (files.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("NO FILES");
                }
                for (int i = 0; i < files.Count; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{dirs.Count + i}. ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(files[i].Name);
                    Console.Write($" ");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    string sizeFormat = Utils.FormatByteSize(files[i].Size);
                    Console.Write($"({sizeFormat})");

                    string localFile = storageMapper.GetLocalPath(files[i].RelativePath);                    
                    if (File.Exists(localFile))
                    {
                        FileInfo fileInfo = new FileInfo(localFile);
                        long localFileSize = fileInfo.Length;
                        float downloadedPercentage = (float)localFileSize / files[i].Size * 100;
                        if (localFileSize == files[i].Size)
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write(" - ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"100.00%");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write(" - ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine($"{downloadedPercentage:F2}%");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write(" - ");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"N/A");
                    }
                }

                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine("D X - Download X");
                Console.WriteLine("E X or X - Enter X");
                Console.WriteLine("U - Go up");
                Console.WriteLine("R - Reload");
                Console.WriteLine("Q - Quit browser");

                string input = Console.ReadLine();
                if (input == null)
                {
                    continue;
                }
                BrowserInput browserInput;
                try
                {
                    browserInput  = new BrowserInput(input);
                }
                catch(Exception)
                {
                    continue;
                }

#if DEBUG
                for (int i = 0; i < browserInput.EntryIndexes.Length; i++)
                {
                    Console.Write(browserInput.EntryIndexes[i]);
                    Console.Write(' ');                    
                }
                Console.WriteLine();
#endif

                switch (browserInput.Command.ToUpper())
                {
                    case "D":
                        for (int i = 0; i < browserInput.EntryIndexes.Length; i++)
                        {
                            string entryName;
                            int index = browserInput.EntryIndexes[i];
                            if (index < dirs.Count)
                            {
                                entryName = dirs[index].RelativePath;
                            }
                            else
                            {
                                entryName = files[index - dirs.Count].RelativePath;
                            }
                            Download(client, preferences, clientLogger, entryName);
                        }
                        Console.WriteLine("Press any key to return to Browser mode...");
                        Console.ReadKey();
                        break;
                    case "E":
                        // TODO remoteViewer.EnterByAbsolutePath(entryName);
                        int enterIndex = browserInput.EntryIndexes[0];
                        if (enterIndex < dirs.Count)
                        {
                            pathStack.Push(current);
                            current = dirs[enterIndex];
                        }
                        else
                        {
                            // TODO May be start downloading here?
                        }

                        break;
                    case "U":
                        // TODO remoteViewer.GoUp();
                        if (pathStack.Count > 0)
                        {
                            current = pathStack.Pop();
                        }
                        break;
                    case "R":
                        rootDigest = client.GetDirectoryDigest(fileName, preferences.BrowserRecursionLevel);                       
                        string currentPath = current.RelativePath;
                        pathStack.Clear();
                        current = rootDigest;
                        string[] pathSegments = currentPath.Split('\\');
                        int depth = 0;
                        while (current.RelativePath != currentPath)
                        {
                            pathStack.Push(current);
                            if(!current.IsLoaded)
                            {
                                Console.WriteLine("Loading...");
                                //DirectoryDigest digest = client.GetDirectoryDigest(ip, port, current.RelativePath, 1 );
                                DirectoryDigest digest = client.GetDirectoryDigest(current.RelativePath, preferences.BrowserRecursionLevel);
                                current.LoadEntriesFrom(digest);
                            }
                            var subDirs = current.GetSubDirectories();
                            var nextDir = subDirs.First(d => d.Name == pathSegments[depth]);
                            depth++;
                            current = nextDir;                            
                        }
                        break;
                    case "Q":
                        return;
                }
            }
        }

        internal static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            internal static extern Boolean AllocConsole();
            [DllImport("kernel32.dll")]
            internal static extern Boolean FreeConsole();
        }


        static void StartGui(ModeOfOperation mode, Preferences preferences)
        {
            Form startupForm = null;
            switch (mode)
            {
                case ModeOfOperation.Browser:
                    
                    break;
                case ModeOfOperation.Server:
                    startupForm = new ServerForm(preferences);
                    break;
                default:
                    Console.WriteLine("GUI does not support this mode of operation");
                    break;
            }

            if(startupForm == null)
            {
                return;
            }

            NativeMethods.FreeConsole();
            Application.EnableVisualStyles();
            Application.Run(startupForm);
        }

        static void PrintHelp()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Console.WriteLine($"BlockShare {version}");

            Console.WriteLine("BlockShare [OPTIONS] CONFIG");

            Console.WriteLine("Available options:");
            Console.WriteLine("Short\tLong\tProperty name");
            foreach (var alias in PreferencesManager<Preferences>.GetCommandLineAliases())
            {
                string c = alias.CharAlias != null ? alias.CharAlias.Value.ToString() : "-";
                string s = alias.StringAlias != null ? alias.StringAlias : "-";
                Console.WriteLine($"{c}\t{s}\t{alias.PropertyInfo.Name}");
            }
        }


        static void Main(string[] args)
        {
            PreferencesManager<Preferences> preferencesManager = new PreferencesManager<Preferences>();

#if DEBUG
            foreach(string arg in args)
            {
                Console.WriteLine(arg);
            }
#endif

            if (args.Length == 0 || args.Length % 2 == 0)
            {
                Console.WriteLine($"Incorrect number of arguments: {args.Length}\n");
                PrintHelp();
                return;
            }
            
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("Help requested\n");
                PrintHelp();
                return;
            }

            string configPath = args[args.Length - 1];
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"{configPath}: file not found");
                string defConfPath = Path.Combine(Environment.CurrentDirectory, "BlockShare.config");
                preferencesManager.SavePreferences(defConfPath);
                Console.WriteLine($"Default config file generated on {defConfPath}\n");
                PrintHelp();
                return;
            }

            try
            {
                preferencesManager.LoadPreferences(configPath);
            }
            catch(RequiredOptionMissingException ex)
            {
                Console.WriteLine($"Failed to load preferences: {ex.Message}");
                return;
            }

            preferencesManager.ParseCommandLine(args);

            var missingOptions = preferencesManager.GetMissingRequiredOptions();
            if(missingOptions.Count != 0)
            {
                Console.WriteLine("Required options missing in both config file and command line arguments: ");
                foreach(var option in missingOptions)
                {
                    Console.WriteLine(option);
                }

                Console.WriteLine();
                PrintHelp();
                return;
            }

            FileInfo configFileInfo = new FileInfo(configPath);

            Environment.CurrentDirectory = configFileInfo.DirectoryName;

            Preferences preferences = preferencesManager.Preferences;

#if ENSURE_SECURITY
            if(preferences.SecurityPreferences == null || preferences.SecurityPreferences.Method == SecurityMethod.None)
            {
                Console.WriteLine("BlockShare was compiled with ENSURE_SECURITY flag. Enable any security method via config.");
                return;
            }
#endif

            ModeOfOperation mode = preferences.Mode;

            if(mode == ModeOfOperation.None)
            {
                Console.WriteLine($"Mode of operation not selected");
                PrintHelp();
                return;
            }

            if (mode == ModeOfOperation.Dehash)
            {
                string storagePath = preferences.ServerStoragePath;
                if (!Directory.Exists(storagePath))
                {
                    Console.WriteLine($"Directory {storagePath} does not exist");
                }

                Utils.Dehash(storagePath);
                return;
            }

            if (mode == ModeOfOperation.Prehash)
            {
                Prehash(preferences);
                return;
            }

            ConsoleLogger serverLogger = new ConsoleLogger("[SERVER]");
            ConsoleLogger clientLogger = new ConsoleLogger("[CLIENT]");

            if (preferences.EnableGui)
            {
                StartGui(mode, preferences);
                return;
            }

            if (mode == ModeOfOperation.Server)
            {
                BlockShareServer server = new BlockShareServer(preferences, serverLogger);
                server.OnHashingProgressChanged += (serverRef, fileNameRef, progress) => serverLogger.OnHashingProgressChanged(fileNameRef, progress);
                server.OnHashingFinished += (serverRef, fileNameRef) => serverLogger.OnHashingFinished(fileNameRef);
                server.StartServer();
            }

            if (mode == ModeOfOperation.Client)
            {
                BlockShareClient client = new BlockShareClient(preferences, clientLogger);
                client.OnHashingProgressChanged += (clientRef, fileNameRef, progress) => clientLogger.OnHashingProgressChanged(fileNameRef, progress);
                client.OnHashingFinished += (clientRef, fileNameRef) => clientLogger.OnHashingFinished(fileNameRef);
                client.OnBlockDownloaded += (clientRef, eventData) => clientLogger.OnBlockDownloaded(eventData);
                client.OnDownloadingFinished += (clientRef, fileNameRef) => clientLogger.OnDownloadingFinished(fileNameRef);
                Download(client, preferences, clientLogger, preferences.ClientStartupPath);
            }

            if (mode == ModeOfOperation.Browser)
            {
                //BlockShareClientOld client = new BlockShareClientOld(preferences, clientLogger);
                BlockShareClient client = new BlockShareClient(preferences, clientLogger);
                client.OnHashingProgressChanged += (clientRef, fileNameRef, progress) => clientLogger.OnHashingProgressChanged(fileNameRef, progress);
                client.OnHashingFinished += (clientRef, fileNameRef) => clientLogger.OnHashingFinished(fileNameRef);
                client.OnBlockDownloaded += (clientRef, eventData) => clientLogger.OnBlockDownloaded(eventData);
                client.OnDownloadingFinished += (clientRef, fileNameRef) => clientLogger.OnDownloadingFinished(fileNameRef);

                Browser(client, preferences, clientLogger, preferences.ClientStartupPath);
            }            

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }
    }
}
