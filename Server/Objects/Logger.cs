/*
 * Author : Dylan Shouldice-Jacobs
 * Purpose: To help debug the 2 project application, simply has a queue that gets added to when an instantiated Logger object Log() is invoked
 *          Which will be checked by another method that handles stuff in queue
 */


using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class Logger
    {
        private string logDir = string.Empty;
        private string logFile = string.Empty;
        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();


        public Logger()
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

        /*
        *  Input   : path to look in, and the file name to look for
        *  Process : Looks for files inside of a dir and returns the amount with a certian name
        *  Output  : num of files with name
        */
        public static int CountFilesWithName(string directoryPath, string partialFileName)
        {
            return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                           .Count(f => Path.GetFileName(f).StartsWith(partialFileName));
        }

        /*
        *  Input   : message to be added to queue
        *  Process : adds a new message to queue
        *  Output  : NONE
        */
        public void Log(string message)
        {
            logQueue.Enqueue($"{DateTime.Now} - {message}");
        }

        public void Stop()
        {
            cts.Cancel();
        }

        /*
        *  Input   : NONE
        *  Process : Adds messages in queue to the log file and the console
        *  Output  : NONE
        */
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
