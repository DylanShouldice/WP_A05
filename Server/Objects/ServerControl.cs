/*
 * Author : Dylan Shouldice-Jacobs
 * Purpose: The purpose of this class is to house the neccesarry methods to control the server and handle requests from clients.
 * 
 */


using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class ServerControl
    {
        private readonly TcpListener listener;
        private readonly ConcurrentDictionary<int, Game> currentGames = new ConcurrentDictionary<int, Game>();
        private readonly ConcurrentQueue<TcpClient> connectionQueue = new ConcurrentQueue<TcpClient>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private volatile bool isShuttingDown = false;
        private string gameDir;
        private int clientsConnected = 1;
        private int totalUsers;


        // RESPONSE

        public const int STRING_AND_WORD_COUNT = 1;
        public const int WORD_COUNT = 2;
        public const int DENY = 3;
        public const int OPEN_REPLAY_PROMPT = 4;
        public const int OPEN_EXIT_PROMPT = 5;
        public const int SERVER_SHUTDOWN = 7;

        // REQUEST

        public const int FIRST_CONNECT = 1;
        public const int GAME_MSG = 2;
        public const int CLIENT_TRYING_TO_LEAVE = 3;
        public const int CLIENT_OUT_OF_TIME = 4;
        public const int CLIENT_LOVES_GAME = 5;
        public const int BYE_CLIENT = 6;





        public ServerControl(string ip, int port)
        {
            Directory.CreateDirectory("gameDir");
            this.gameDir = "gameDir";
            listener = new TcpListener(IPAddress.Parse(ip), port);
            Logger.InitalizeLogger();
        }


        /*
        *  Input   : NONE
        *  Process : Starts the threads needed for the server to work, and then waits for connections and adds them to a queue, so we can have multiple requests.
        *  Output  : Request being added to queue
        */
        public async Task StartServer() // Start server, accept clients
        {
            listener.Start();
            _ = Task.Run(() => MonitorServerStopInput(), cts.Token);
            Thread connectionHandler = new Thread(AcceptConnections);
            connectionHandler.Start();

            Logger.Log("SERVER STARTED");
            Logger.Log($"Waiting for clients");

            while (!isShuttingDown)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    connectionQueue.Enqueue(client);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.Log($"Exception caught -- ServerControl.StopServer() -- {e}");
                }
            }
        }


        /*
        *  Input   : NONE
        *  Process : Checks if the queue has any messages pending, if so it creates a task to handle the request.
        *            It only stops waiting for connections after the last client connects. - Because the 
        *  Output  : Tasks to handle clients
        */
        internal void AcceptConnections()
        {
            while (!isShuttingDown)
            {
                TcpClient newClient = new TcpClient();
                if (connectionQueue.TryDequeue(out newClient))
                {
                    Task.Run(() => HandleClient(newClient, cts.Token));
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }


        /*
        *  Input   : NONE
        *  Process : Gets started at server runtime. Waits for someone on the host computer to press 'c' in the console where the server is running
        *            if this happens it will trigger the server stop gracefully
        *  Output  : NONE
        */
        private async void MonitorServerStopInput()
        {
            while (!cts.IsCancellationRequested)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.C) // Check for 'c' key press
                {
                    Logger.Log($"Server Shutdown Initiated");
                    cts.Cancel();
                }
            }
            while (totalUsers > 0)
            {
                Logger.Log($"Total Users {totalUsers}");
                Thread.Sleep(100);
            }

            isShuttingDown = true;
            listener.Stop();
            Logger.Stop();
            currentGames.Clear();
        }




        /* ---------- CLIENT HANDLING ---------- */

        /*
        *  Input   : TcpClient - user - client with request that needs processing
        *            CancelToken - cToken - is canceled when host presses. Which will not allow messages to be processed normally. 
        *            Instead all responses will be of type DENY
        *  Process : Checks the message type then sends it to a function to be handled. Either to FirstConnection, or SubsuqentConnection. 
        *            Unless the cToken is cancelled
        *  Output  : Appropriate response fo the clients request
        */
        private async Task HandleClient(TcpClient user, CancellationToken cToken)
        {
            Game game = null;
            try
            {
                Logger.Log($"Total Users {totalUsers}");

                string responseContent = string.Empty;
                string[] msg = await ReadMessage(user);
                int respType = 0;
                int reqType = int.Parse(msg[0]);

                if (reqType == FIRST_CONNECT && !cts.IsCancellationRequested)
                {
                    totalUsers++;
                    HandleFirstConnect(out game, out responseContent, out respType, msg);
                }
                else
                {
                    currentGames.TryGetValue(int.Parse(msg[1]), out game);
                    if (!cts.IsCancellationRequested)
                    {
                        Logger.Log($"Client {{ {game.clientId} }} reconnected. Total {{ {totalUsers} }}");
                        HandleSubsuqentRequests(reqType, msg, game, out responseContent, out respType);
                    }
                }

                if (!cts.IsCancellationRequested)
                {
                    SendMessage(user, respType, game.clientId, responseContent);
                }
                else
                {
                    responseContent = string.Empty;
                    respType = DENY;
                    SendMessage(user, respType, int.Parse(msg[1]), responseContent);
                    await ReadMessage(user);
                    currentGames.TryRemove(int.Parse(msg[1]), out _);
                    totalUsers--;
                }
            }
            catch (IOException)
            {
                Logger.Log($"Client Disconnected Total {{ {totalUsers} }}");
            }
            catch (Exception e)
            {
                Logger.Log($"Exception caught -- ServerControl.HandleClient() -- {e.Message}");
            }
            finally
            {
                CloseConnection(user);
            }
        }
        /*
        *  Input   : Game - user - game to be closed
        *  Process : Closes an open connection
        *  Output  : NONE
        */
        private void CloseConnection(TcpClient user)
        {
            Logger.Log($"Client connection closed");
            user.Close();
        }
        /*
        *  Input   : takes in the msg, and the variables it needs to send output to. as well as a game to use game.Play()
        *  Process : Completes request sent by clients after their first connection
        *  Output  : The response message into the variables with 'out'
        */
        private void HandleSubsuqentRequests(int reqType, string[] msg, Game game, out string responseContent, out int respType)
        {
            responseContent = string.Empty;
            respType = 0;

            switch (reqType)
            {
                case GAME_MSG:
                    responseContent = game.Play(msg);
                    respType = game.remainingWords == 0 ? OPEN_REPLAY_PROMPT : GAME_MSG; // short-hand if statement if remWord == 0, open_replay else game_msg
                    break;
                case CLIENT_TRYING_TO_LEAVE:
                    responseContent = " ";
                    respType = OPEN_EXIT_PROMPT;
                    break;
                case CLIENT_OUT_OF_TIME:
                    respType = OPEN_REPLAY_PROMPT;
                    break;
                case CLIENT_LOVES_GAME:
                    game.InitalizeGame();
                    if (game.prevWordPool == game.currentWordPool)
                    {
                        game.InitalizeGame();
                    }
                    game.prevWordPool = game.currentWordPool;
                    respType = STRING_AND_WORD_COUNT;
                    responseContent = $"{game.currentWordPool} {game.remainingWords}";
                    break;
                case BYE_CLIENT:
                    currentGames.TryRemove(int.Parse(msg[1]), out _);
                    totalUsers--;
                    break;
            }
        }

        /*
        *  Input   : Game - game - game object to be created
        *            then the rest is the response that is built.
        *  Process : Goes through the process of creating or refreshing a game object
        *  Output  : a new Game and the message to be sent
        */
        private void HandleFirstConnect(out Game game, out string responseContent, out int respType, string[] msg)
        {
            game = new Game(gameDir, GenerateId(msg[1]));
            Logger.Log($"New game made name: {msg[1]}");
            game.InitalizeGame();
            if (game.prevWordPool == game.currentWordPool)
            {
                game.InitalizeGame();
            }
            game.prevWordPool = game.currentWordPool;
            game.clientName = msg[1];
            responseContent = $"{game.currentWordPool} {game.remainingWords}";
            currentGames.TryAdd(game.clientId, game);
            respType = STRING_AND_WORD_COUNT;
        }

        /*
        *  Input   : string - nameToHash - username that will be turned into ID
        *  Process : turns string into hash for hashTable
        *  Output  : new Id
        */
        private int GenerateId(string nameToHash)
        {
            if (currentGames.ContainsKey(nameToHash.GetHashCode() % 256))
            {
                nameToHash = Guid.NewGuid().ToString();
            }
            return (nameToHash.GetHashCode() % 256); // Ensures it will not take up more then 1 character
        }



        /* ---------- SEND / RECIEVE ---------- */

        /*
        *  Input   : client to be sent to, and the message
        *  Process : Sends message to client
        *  Output  : NONE
        */
        public void SendMessage(TcpClient client, int respType, int clientId, string content)
        {
            string toSend = $"{respType} {clientId} {content}";
            Logger.Log($"Sending Message :{toSend}");
            var buffer = Encoding.ASCII.GetBytes(toSend);
            client.GetStream().Write(buffer, 0, buffer.Length);
        }

        /*
         *  Input   : client to read from
         *  Process : Reads a message 
         *  Output  : parsed message in string[]
         */
        public async Task<string[]> ReadMessage(TcpClient client)
        {
            Byte[] buffer = new Byte[1024];
            NetworkStream stream = client.GetStream();
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string logString = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            Logger.Log($"Message Recieved {logString}");
            return logString.Split(' ');
        }


    }
}