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
            private double previousProgress;
            private double reportingStep = 0.01f;
            public ConsoleProgressReporter(string message)
            {
                this.message = message;
            }
            public void ReportFinishing(object sender, bool success)
            {
                Console.WriteLine(message + ": finished");
            }

            public void ReportProgress(object sender, double progress)
            {
                if(progress - previousProgress < reportingStep)
                {
                    return;
                }
                previousProgress = progress;
                Console.WriteLine(message + $": {progress*100.0:0.00}");
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
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                client.DownloadFile(ip, port, fileName, new ConsoleProgressReporter("Hashing: "), new ConsoleProgressReporter("Downloading: "));
                long millis = stopwatch.ElapsedMilliseconds;
                stopwatch.Stop();
                double seconds = (double)millis / 1000;
                FileInfo fileInfo = new FileInfo(Path.Combine(storagePath, fileName));
                double bytesPerSecond = fileInfo.Length / seconds;
                double megabytesPerSecond = bytesPerSecond / 1024 / 1024;
                Console.WriteLine($"Speed: {megabytesPerSecond} Mb/s");              
            }

            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }
    }
}
