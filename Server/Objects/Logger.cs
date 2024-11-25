/*
 * Author : Dylan Shouldice-Jacobs
 * Purpose: To help debug the 2 project application, simply has a queue that gets added to when an instantiated Logger object Log() is invoked
 *          Which will be checked by another method that handles stuff in queue
 */

namespace Server
{
    internal class Logger
    {
        private string logFile = string.Empty;
        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();


        public Logger()
        {
            this.logFile = ConfigurationSettings.AppSettings["LogFilePath"];

            Task.Run(() => ProcessMessageQueue());
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
                        File.AppendAllText(logFile, message + "\n");
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
