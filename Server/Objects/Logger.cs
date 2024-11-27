/*
 * Author : Dylan Shouldice-Jacobs
 * Purpose: To help debug the 2 project application, simply has a queue that gets added to when an instantiated Logger object Log() is invoked
 *          Which will be checked by another method that handles stuff in queue
 */


using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public static class Logger
    {
        private static string logFile = string.Empty;
        private static readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();


        public static void InitalizeLogger()
        {
            logFile = ConfigurationSettings.AppSettings["LogFilePath"];
            Task.Run(() => ProcessMessageQueue());
        }

        /*
        *  Input   : message to be added to queue
        *  Process : adds a new message to queue
        *  Output  : NONE
        */
        public static void Log(string message)
        {
            logQueue.Enqueue($"{DateTime.Now} - {message}\n");
        }

        public static void Stop()
        {
            cts.Cancel();
        }

        /*
        *  Input   : NONE
        *  Process : Adds messages in queue to the log file and the console
        *  Output  : NONE
        */
        public static void ProcessMessageQueue()
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    if (logQueue.TryDequeue(out string message))
                    {
                        File.AppendAllText(logFile, message);
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
