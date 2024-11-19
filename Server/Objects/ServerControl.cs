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
        private readonly Logger logger;
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
            Thread connectionHandler = new Thread(AcceptConnections);
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
            totalUsers++;
            clientsConnected++;
            Game game = null;
            try
            {
                string responseContent = string.Empty;
                bool initGame = false;
                string[] msg = await ReadMessage(user);
                int respType = 0;
                int reqType = int.Parse(msg[0]);

                if (reqType == FIRST_CONNECT) // good
                {
                    logger.Log($"New Client, Total #{totalUsers}");
                    game = new Game(gameDir, GenerateId(msg[1]));
                    game.InitalizeGame();
                    responseContent = $"{game.currentWordPool} {game.remainingWords}";
                    currentGames.TryAdd(game.clientId, game);                                       // Add game to list
                    respType = FIRST_CONNECT;
                }
                else
                {
                    currentGames.TryGetValue(int.Parse(msg[1]), out game);
                    if (reqType == GAME_MSG)
                    {
                        logger.Log($"Client reconnected with ID: {msg[1]}, Total #{totalUsers}");
                        responseContent = await Task.Run(() => game.Play(msg));
                        if (game.remainingWords == 0) // check if that was last character
                        {
                            respType = OPEN_REPLAY_PROMPT;
                        }
                        else
                        {
                            respType = GAME_MSG;
                        }
                    }
                    else if (reqType == CLIENT_TRYING_TO_LEAVE) // release client stuff
                    {
                        logger.Log($"Client reconnected with ID: {msg[1]}");
                        responseContent = " ";
                        respType = 5; // send message that will make client prompt user 'are you sure
                    }
                    if (reqType == CLIENT_LOVES_GAME || initGame)
                    {
                        game.InitalizeGame();
                        responseContent = $"{game.currentWordPool} {game.remainingWords}";
                    }
                    else if (reqType == BYE_CLIENT) // Play again
                    {
                        currentGames.TryRemove(int.Parse(msg[1]), out _);
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
                user.Close();
                clientsConnected--;
                logger.Log($"Client : {user.Client.RemoteEndPoint} has disconnected");
            }
        }

        private void RestartGame(Game game)
        {

        }

        private int GenerateId(string nameToHash)
        {
            logger.Log(nameToHash);
            return (nameToHash.GetHashCode() % 256);
        }

        public void SendMessage(TcpClient client, int respType, int clientId, string content)
        {
            string toSend = $"{respType} {clientId} {content}"; // ensure content is able to parsed by ' '
            logger.Log($"Sending Message :{toSend}");
            var buffer = Encoding.ASCII.GetBytes(toSend);
            client.GetStream().Write(buffer, 0, buffer.Length);
        }

        public void test()
        {

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




//responseContent = " ";
//respType = 4;
//SendMessage(user, respType, game.clientId, responseContent);
//string[] playAgain = await ReadMessage(user);
//if (playAgain[0] == "0")
//{
//    game.InitalizeGame();
//    responseContent = $"{game.currentWordPool} {game.remainingWords}";
//    SendMessage(user, respType, game.clientId, responseContent);
//}
//else
//{
//    SendMessage(user, respType, game.clientId, responseContent);
//    currentGames.TryRemove(game.clientId, out _);
//}



//logger.Log($"Client reconnected with ID: {msg[1]}");
//currentGames.TryGetValue(int.Parse(msg[1]), out game);
//responseContent = " ";
//respType = 5; // send message that will make client prompt user 'are you sure
//SendMessage(user, respType, game.clientId, responseContent);
//string[] conformation = await ReadMessage(user);
//if (conformation[0] == "0")
//{
//    SendMessage(user, respType, game.clientId, responseContent);
//    currentGames.TryRemove(game.clientId, out _);
//}