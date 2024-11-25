using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server_As_A_Service
{
    internal class Logger
    {
        private string logDir = string.Empty;
        private string logFile = string.Empty;
        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();


        public Logger ()
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");

            }
            this.logDir = "Logs";
            string numOfLogs = CountFilesWithName(logDir, "serverLog").ToString();
            this.logFile = "serverLog_" + numOfLogs;

            Task.Run(() => ProcessMessageQueue());
        }

        public static int CountFilesWithName(string directoryPath, string partialFileName)
        {
            return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                           .Count(f => Path.GetFileName(f).StartsWith(partialFileName));
        }

        public void Log (string message)
        {
            logQueue.Enqueue($"{DateTime.Now} - {message}");
        }

        public void Stop()
        {
            cts.Cancel();
        }

        public void ProcessMessageQueue()
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    if (logQueue.TryDequeue(out string message))
                    {
                        Console.WriteLine(message);
                        File.AppendAllText(logDir + "/" + logFile, message + "\n");
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception caught in 'ProcessMessageQueue()' : {e.Message}");
                }
            }
        }
    }
}
