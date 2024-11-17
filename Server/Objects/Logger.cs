using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class Logger
    {
        private string logDir = string.Empty;
        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();


        public Logger (string serverLogDir)
        {
            this.logDir = serverLogDir;
            Task.Run(() => ProcessMessageQueue());
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
                        File.AppendAllText(logDir, message + "\n");
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
