﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server_As_A_Service
{


    internal class ServerControl : ServiceBase
    {
        private readonly TcpListener listener;
        private readonly ConcurrentDictionary<int, Game> currentGames = new ConcurrentDictionary<int, Game>();
        private readonly ConcurrentQueue<TcpClient> connectionQueue = new ConcurrentQueue<TcpClient>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly Logger logger;
        private volatile bool isShuttingDown = false;
        private string gameDir;
        private int clientsConnected;
        private int totalUsers;

        // Communcation

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

        Thread t;



        public ServerControl(string ip, int port)
        {
            Directory.CreateDirectory("gameDir");
            this.gameDir = "gameDir";
            //listener = new TcpListener(IPAddress.Parse(ip), port); --I think is causing exception
            logger = new Logger();
        }



        public async Task StartServer() // Start server, accept clients
        {
            listener.Start();
            _ = Task.Run(() => MonitorServerStopInput(), cts.Token);
            Thread connectionHandler = new Thread(AcceptConnections);
            connectionHandler.Start();

            logger.Log("SERVER STARTED");
            logger.Log($"Waiting for clients");

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
                    logger.Log($"Exception caught -- ServerControl.StopServer() -- {e.Message}");
                }
            }
        }


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


        private async void MonitorServerStopInput()
        {
            while (!cts.IsCancellationRequested)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.C) // Check for 'c' key press
                {
                    cts.Cancel();
                }
            }
        }
        /* ---------- CLIENT HANDLING ---------- */
        private async Task HandleClient(TcpClient user, CancellationToken cToken)
        {
            Game game = null;
            try
            {
                string responseContent = string.Empty;
                string[] msg = await ReadMessage(user);
                int respType = 0;
                int reqType = int.Parse(msg[0]);

                if (reqType == FIRST_CONNECT && !cts.IsCancellationRequested)
                {
                    HandleFirstConnect(user, out game, out responseContent, out respType, msg);
                }
                else
                {
                    currentGames.TryGetValue(int.Parse(msg[1]), out game);
                    if (!cts.IsCancellationRequested)
                    {
                        logger.Log($"Client {{ {game.clientId} }} reconnected");
                        HandleSubsuqentRequests(user, reqType, msg, game, out responseContent, out respType);
                    }
                    else
                    {
                        responseContent = string.Empty;
                        respType = DENY;
                    }
                }

                SendMessage(user, respType, game.clientId, responseContent);
            }
            catch (Exception e)
            {
                logger.Log($"Exception caught -- ServerControl.HandleClient() -- {e}");
            }
            finally
            {
                CloseConnection(game);
            }
        }

        private void CloseConnection(Game user)
        {
            logger.Log($"Client {{ {user.clientId} }} connection closed");
            user.client.Close();
        }

        private void HandleSubsuqentRequests(TcpClient user, int reqType, string[] msg, Game game, out string responseContent, out int respType)
        {
            responseContent = string.Empty;
            respType = 0;
            game.client = user;

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
                    respType = STRING_AND_WORD_COUNT;
                    responseContent = $"{game.currentWordPool} {game.remainingWords}";
                    break;
                case BYE_CLIENT:
                    currentGames.TryRemove(int.Parse(msg[1]), out _);
                    break;
            }
        }


        private void HandleFirstConnect(TcpClient user, out Game game, out string responseContent, out int respType, string[] msg)
        {
            game = new Game(user, gameDir, GenerateId(msg[1]));
            logger.Log($"New game made name: {msg[1]}");
            game.InitalizeGame();
            game.clientName = msg[1];
            responseContent = $"{game.currentWordPool} {game.remainingWords}";
            currentGames.TryAdd(game.clientId, game);
            respType = STRING_AND_WORD_COUNT;
        }


        private int GenerateId(string nameToHash)
        {
            return (nameToHash.GetHashCode() % 256); // Ensures it will not take up more then 1 character
        }



        /* ---------- SEND / RECIEVE ---------- */


        public void SendMessage(TcpClient client, int respType, int clientId, string content)
        {
            string toSend = $"{respType} {clientId} {content}";
            logger.Log($"Sending Message :{toSend}");
            var buffer = Encoding.ASCII.GetBytes(toSend);
            client.GetStream().Write(buffer, 0, buffer.Length);
        }


        public async Task<string[]> ReadMessage(TcpClient client)
        {
            Byte[] buffer = new Byte[1024];
            NetworkStream stream = client.GetStream();
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string logString = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            logger.Log($"Message Recieved {logString}");
            return logString.Split(' ');
        }

        protected override void OnStart(string[] args)
        {
            t.Start();
        }

        protected async override void OnStop()
        {
            logger.Log("SERVER STOP INITIATED");
            cts.Cancel();
            while (totalUsers > 0)
            {

            }
            logger.Log("SERVER STOPPED");
        }

        private void InitializeComponent()
        {
            // 
            // ServerControl
            // 
            this.ServiceName = "ServerControl";

        }
    }
}