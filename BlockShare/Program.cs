﻿using BlockShare.BlockSharing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockShare.BlockSharing.HashMapping;
using BlockShare.BlockSharing.RemoteFileSystem;

namespace BlockShare
{
    class Program
    {
        private class ConsoleLogger : ILogger
        {
            public string Prefix { get; set; }
            
            public ConsoleLogger(string prefix)
            {
                Prefix = prefix;
            }
            public void Log(string message)
            {
                Console.WriteLine(Prefix + " " + message);
            }
        }

        private class ConsoleProgressReporter : IProgressReporter
        {
            private string message;
            private double overallProgress;
            private List<double> previousProgressList = new List<double>();
            private double reportingStep = 0.01f;
            public ConsoleProgressReporter(string message)
            {
                this.message = message;
            }
            public void ReportFinishing(object sender, bool success, int jobId)
            {
                Console.WriteLine(message + ": finished");
            }

            public void ReportOverallProgress(object sender, double progress)
            {
                if (progress - overallProgress < reportingStep)
                {
                    return;
                }
                overallProgress = progress;
                Console.WriteLine(message + $": {progress * 100.0:0.00}");

            }

            public void ReportOverallFinishing(object sender, bool success)
            {
                Console.WriteLine(message + " (all jobs): finished");
            }

            public void ReportProgress(object sender, double progress, int jobId)
            {
                while (previousProgressList.Count <= jobId)
                {
                    previousProgressList.Add(0.0f);
                }

                if(progress - previousProgressList[jobId] < reportingStep)
                {
                    return;
                }
                previousProgressList[jobId] = progress;
                Console.WriteLine(message + $"(job {jobId}): {progress*100.0:0.00}");
            }
        }

        static void Download(string ip, string portStr, Preferences preferences, ILogger clientLogger, string fileName)
        {
            if (fileName[0] == Path.DirectorySeparatorChar || fileName[0] == Path.AltDirectorySeparatorChar)
            {
                fileName = fileName.Remove(0, 1);
            }

            BlockShareClient client = new BlockShareClient(preferences, clientLogger);

            int port = int.Parse(portStr);
            Stopwatch sw = Stopwatch.StartNew();
            client.DownloadFile(ip, port, fileName, new ConsoleProgressReporter("Hashing: "), new ConsoleProgressReporter("Downloading: "));
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

            NetStat clientNetStat = client.GetClientNetStat;

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

        static void Browser(RemoteFileSystemViewer remoteViewer, string ip, string portStr, Preferences preferences, ILogger clientLogger, string fileName)
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Current directory: {remoteViewer.CurrentDirectory.RemoteFullPath}\n");
                
                Console.WriteLine("Directories: \n");
                var dirs = remoteViewer.ListCurrentSubDirectories();
                if (dirs.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("NO DIRECTORIES");
                }
                for (int i = 0; i < dirs.Length; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{i}. ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(dirs[i].Name);
                    Console.Write($" ");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    string sizeFormat = Utils.FormatByteSize(dirs[i].CalculateSize());
                    Console.WriteLine($"({sizeFormat})");
                }

                Console.WriteLine("\nFiles: \n");
                var files = remoteViewer.ListCurrentFiles();
                if (files.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("NO FILES");
                }
                for (int i = 0; i < files.Length; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{dirs.Length + i}. ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(files[i].Name);
                    Console.Write($" ");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    string sizeFormat = Utils.FormatByteSize(files[i].Size);
                    Console.WriteLine($"({sizeFormat})");
                }

                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine("D X - Download X");
                Console.WriteLine("E X or X - Enter X");
                Console.WriteLine("U - Go up");
                Console.WriteLine("Q - Quit browser");

                string input = Console.ReadLine();
                if (input == null)
                {
                    continue;
                }
                string[] inputWords = input.Split(' ');

                string entryName = string.Empty;
                if (inputWords.Length == 2)
                {
                    bool ok = int.TryParse(inputWords[1], out int index);
                    if (!ok)
                    {
                        continue;
                    }

                    if (index < dirs.Length)
                    {
                        entryName = dirs[index].RemoteFullPath;
                    }
                    else
                    {
                        entryName = files[index - dirs.Length].RemoteFullPath;
                    }
                }
                else
                {
                    bool ok = int.TryParse(inputWords[0], out int enterIndex);
                    if (ok)
                    {
                        if (enterIndex < dirs.Length)
                        {
                            entryName = dirs[enterIndex].RemoteFullPath;
                        }
                        else
                        {
                            entryName = files[enterIndex - dirs.Length].RemoteFullPath;
                        }
                        //remoteViewer.EnterFromCurrentDirectory(entryName);
                        remoteViewer.EnterByAbsolutePath(entryName);
                        continue;
                    }
                }

                switch (inputWords[0].ToUpper())
                {
                    case "D":
                        Download(ip, portStr, preferences, clientLogger, entryName);
                        Console.WriteLine("Press any key to return to Browser mode...");
                        Console.ReadKey();
                        break;
                    case "E":
                        //remoteViewer.EnterFromCurrentDirectory(entryName);
                        remoteViewer.EnterByAbsolutePath(entryName);
                        break;
                    case "U":
                        remoteViewer.GoUp();
                        break;
                    case "Q":
                        return;
                }
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("[1] BlockSharing.exe dehash <path>");
                Console.WriteLine("[2] BlockSharing.exe prehash <path>");
                Console.WriteLine("[3] BlockSharing.exe client <server-ip> <server-port> <storage path> [remote file]");
                Console.WriteLine("[4] BlockSharing.exe browser <server-ip> <server-port> <storage path> [starting dir]");
                Console.WriteLine("[5] BlockSharing.exe server <bind-ip> <bind-port> <storage path>");
                return;
            }

            string startupDir = AppDomain.CurrentDomain.BaseDirectory;
            string hashpartStorage = Path.Combine(startupDir, "BlockShare-Hashparts");
            string hashlistStorage = Path.Combine(startupDir, "BlockShare-Hashlists");
            HashMapper hashMapper = new ShaHashMapper(hashpartStorage, hashlistStorage);

            string storagePath;

            string mode = args[0];
            if (mode == "dehash")
            {
                 storagePath = args[1];
                 if (!Directory.Exists(storagePath))
                 {
                    Console.WriteLine($"Directory {storagePath} does not exist");
                 }

                 Utils.Dehash(storagePath);
                 return;
            }

            if (mode == "prehash")
            {
                storagePath = args[1];

                Preferences prehashPreferences = new Preferences();
                prehashPreferences.ServerStoragePath = storagePath;
                prehashPreferences.HashMapper = hashMapper;

                Prehash(prehashPreferences);

                return;
            }

            string ip;
            string portStr;
            string fileName = String.Empty;

            if (mode == "client")
            {
                ip = args[1];
                portStr = args[2];
                storagePath = args[3];

                if (args.Length == 5)
                {
                    fileName = args[4];
                }
                else
                {
                    Console.Write("File name: ");
                    fileName = Console.ReadLine();
                }
            }
            else if (mode == "browser")
            {
                ip = args[1];
                portStr = args[2];
                storagePath = args[3];

                if (args.Length == 5)
                {
                    fileName = args[4];
                }
            }
            else if (mode == "server")
            {
                ip = args[1];
                portStr = args[2];
                storagePath = args[3];
            }
            else
            {
                Console.WriteLine($"Unknown mode: {mode}");
                return;
            }

            Preferences preferences = new Preferences();
            preferences.ServerStoragePath = storagePath;
            preferences.ClientStoragePath = storagePath;
            ConsoleLogger serverLogger = new ConsoleLogger("[SERVER]");
            ConsoleLogger clientLogger = new ConsoleLogger("[CLIENT]");

            preferences.HashMapper = hashMapper;

            if (mode == "server")
            {
                int port = int.Parse(portStr);
                BlockShareServer server = new BlockShareServer(ip, port, preferences, new ConsoleProgressReporter("Hashing: "), serverLogger);
                server.StartServer();
            }
            
            if(mode == "client")
            {
                Download(ip, portStr, preferences, clientLogger, fileName);
            }

            if (mode == "browser")
            {
                BlockShareClient client = new BlockShareClient(preferences, clientLogger);

                int port = int.Parse(portStr);
                RemoteFileSystemViewer remoteViewer = client.Browse(ip, port, fileName);
                if (remoteViewer != null)
                {
                    Browser(remoteViewer, ip, portStr, preferences, clientLogger, fileName);
                }
                else
                {
                    Console.WriteLine("Error during receiving remote file system info");
                }
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }
    }
}
