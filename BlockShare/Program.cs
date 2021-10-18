using BlockShare.BlockSharing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                Console.WriteLine(message + $"(job {jobId}: {progress*100.0:0.00}");
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("[1] Mode: server/client");
                Console.WriteLine("[2] Ip: server bind/client connect");
                Console.WriteLine("[3] Port: server bind/client connect");
                Console.WriteLine("[4] Storage path");
                Console.WriteLine("[5] (For client only) File name");

                return;
            }         

            string mode = args[0];
            string ip = args[1];
            string portStr = args[2];
            string storagePath = args[3];
            string fileName = String.Empty;
            if (mode == "client")
            {
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

            Preferences preferences = new Preferences();
            preferences.ServerStoragePath = storagePath;
            preferences.ClientStoragePath = storagePath;
            ConsoleLogger serverLogger = new ConsoleLogger("[SERVER]");
            ConsoleLogger clientLogger = new ConsoleLogger("[CLIENT]");                    

            if(mode == "server")
            {
                int port = int.Parse(portStr);
                BlockShareServer server = new BlockShareServer(ip, port, preferences, new ConsoleProgressReporter("Hashing: "), serverLogger);
                server.StartServer();
            }
            
            if(mode == "client")
            {
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
                    double efficiency = (double) clientNetStat.Payload /
                                        (clientNetStat.TotalSent + clientNetStat.TotalReceived);
                    double downloadingSpeed = (double) clientNetStat.TotalReceived / (millis / 1000.0f);
                    double efficiencyPercent = efficiency * 100.0f;
                    double downloadingSpeedMibS = downloadingSpeed / 1024 / 1024;
                    Console.WriteLine($"Efficiency: {efficiencyPercent}%");
                    Console.WriteLine($"Speed: {downloadingSpeedMibS} MiB/s");
                }
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }
    }
}
