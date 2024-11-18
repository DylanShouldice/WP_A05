using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using Client;
using System.IO;

namespace Server
{


    internal class ServerControl
    {
        private readonly TcpListener listener;
        private readonly ConcurrentDictionary<int, Game> currentGames = new ConcurrentDictionary<int, Game>();
        private readonly ConcurrentQueue<TcpClient> connectionQueue = new ConcurrentQueue<TcpClient>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly Logger logger;
        private string gameDir;
        private int clientsConnected;
        private int totalUsers;

        // Consts 

        // Communcation

        public const int FIRST_CONNECT = 1;
        public const int GAME_MSG = 2;
        public const int NON_GAME_MSG = 3;
        public const int SERVER_MSG = 4;

        // Client Status

        public const int CONNECTED = 0;
        public const int AWAITING = 1;
        public const int TIME_OUT = 2;
        public const int DISCONNECTED = 3;

        public ServerControl(string ip, int port)
        {
            Directory.CreateDirectory("gameDir");
            this.gameDir = "gameDir";
            listener = new TcpListener(IPAddress.Parse(ip), port);
            logger = new Logger();
        }

        private string GetGameDir()
        {
            Console.WriteLine("Input game directory (folder holding game files)");
            string fileDir = Console.ReadLine();
            if (!Directory.Exists(fileDir))
            {
                Console.WriteLine($"{fileDir} does not exist");
                return string.Empty;
            }
            return fileDir;
        }

        public async Task StartServer() // Start server, accept clients
        {
            listener.Start();
           // Thread admin = new Thread(AdminPanel);
            Thread connectionHandler = new Thread(AcceptConnections);
           // admin.Start();
            connectionHandler.Start();
            
            logger.Log("SERVER STARTED");
            logger.Log($"Waiting for clients");

            while (!cts.IsCancellationRequested)
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

        private async void AdminPanel()
        {
            while (!cts.IsCancellationRequested)
            {
                TextReader standardInput = Console.In;
                TextWriter standardOutput = Console.Out;
                string readAdmin = await standardInput.ReadLineAsync();
                standardOutput.WriteAsync(readAdmin);
                Console.SetOut(standardOutput);
                Console.SetIn(standardInput);
            }
        }

        internal void AcceptConnections()
        {
            while (!cts.IsCancellationRequested)
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

        public void StopServer()
        {
            logger.Log("Server stop initated");
            cts.Cancel();
            listener.Stop();

            foreach (var game in currentGames)
            {
                try
                {

                }
                catch (Exception e)
                {
                    logger.Log($"Exception caught -- ServerControl.StopServer() -- {e.Message}");
                }
            }

            currentGames.Clear();
            logger.Log($"SERVER CLOSING");
        }

        private async Task HandleClient(TcpClient user, CancellationToken cToken)
        {
            logger.Log($"New Connection, Total #{clientsConnected}");
            totalUsers++;
            clientsConnected++;
            Game game = null;
            try
            {
                string responseContent = string.Empty;
                int type = 0;
                string[] msg = await ReadMessage(user);
                if (msg[0] == "1")
                {
                    logger.Log($"New Client, Total #{totalUsers}");
                    game = new Game(gameDir, GenerateId());
                    game.InitalizeGame();
                    currentGames.TryAdd(game.clientId, game);
                    responseContent = $"{game.currentWordPool} {game.remainingWords}";
                    type = 1;
                }
                else if (msg[0] == "2")
                {
                    logger.Log($"Client reconnected with ID: {msg[1]}");
                    currentGames.TryGetValue(int.Parse(msg[1]), out game);
                    responseContent = await Task.Run(() => game.Play(msg));
                    type = 2;
                }
                else if (msg[0] == "3") // Prompt for exit
                {
                    logger.Log($"Client reconnected with ID: {msg[1]}");
                    currentGames.TryGetValue(int.Parse(msg[1]), out game);
                    responseContent = " ";
                    type = 5; // send message that will make client prompt user 'are you sure
                    SendMessage(user, type, game.clientId, responseContent);
                    string[] conformation = await ReadMessage(user);
                    if (conformation[0] == "0")
                    {
                        SendMessage(user, type, game.clientId, responseContent);
                        currentGames.TryRemove(game.clientId, out _);
                    }
                }
                else if (msg[0] == "4") // Play again
                {
                    responseContent = " ";
                    type = 4;
                    SendMessage(user, type, game.clientId, responseContent);
                    string[] playAgain = await ReadMessage(user);
                    if (playAgain[0] == "0")
                    {
                        game.InitalizeGame();
                        responseContent = $"{game.currentWordPool} {game.remainingWords}";
                        SendMessage(user, type, game.clientId, responseContent);
                    }
                    else
                    {
                        SendMessage(user, type, game.clientId, responseContent);
                        currentGames.TryRemove(game.clientId, out _);
                    }
                }
                else
                {
                    logger.Log($"Input not being processed");
                }
                if (msg[0] != "3" || msg[0] != "4")
                {
                    SendMessage(user, type, game.clientId, responseContent);
                }
            }
            catch (Exception e)
            {
                logger.Log($"Exception caught -- ServerControl.HandleClient() -- {e}");
            }
            finally
            {
                user.Close();
                clientsConnected--;
                logger.Log($"Client : {user.Client.RemoteEndPoint} has disconnected");
            }
        }

        private int GenerateId()
        {
            return totalUsers;
        }

        public void SendMessage(TcpClient client, int type, int clientId, string content)
        {
            string toSend = $"{type} {clientId} {content}"; // ensure content is able to parsed by ' '
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
        // TO-DO : Current Task create dynamic start up for server and logger that allows choice of ip, and which dir to get for gameDir,
        // then either create or find file for logDir. But dont nest log dir inside of server, incase you wamt to use it later
    }
}