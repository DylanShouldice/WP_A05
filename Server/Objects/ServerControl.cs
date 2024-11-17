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
    public struct Message
    {
        public string content;
        public int client;
        public int type;

        public Message(string msg)
        {
            Logger log = new Logger("ree");
            content = msg;
            log.Log(content);
            type = int.Parse(msg[0].ToString());
            if (type != 1)
            {
                client = int.Parse(msg[1].ToString());
            }
            else
            {
                client = 0;
            }
        }
    }

    internal class ServerControl
    {
        private readonly TcpListener listener;
        private readonly ConcurrentDictionary<string, Game> currentGames = new ConcurrentDictionary<string, Game>();
        private readonly ConcurrentQueue<TcpClient> connectionQueue = new ConcurrentQueue<TcpClient>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly Logger logger;
        private readonly string gameDir;
        private int clientsConnected;
        private int totalUsers;

        // Consts 

        // Communcation

        public const int FIRST_CONNECT = 1;
        public const int GAME_MSG      = 2;
        public const int NON_GAME_MSG  = 3;
        public const int SERVER_MSG    = 4;

        // Client Status

        public const int CONNECTED = 0;
        public const int AWAITING = 1;
        public const int TIME_OUT = 2;
        public const int DISCONNECTED = 3;

        public ServerControl(string ip, int port, string gameDir)
        {
            this.gameDir = gameDir;
            listener = new TcpListener(IPAddress.Parse(ip), port);
            logger = new Logger("test");
        }

        public ServerControl(string ip, int port)
        {
            listener = new TcpListener(IPAddress.Parse(ip), port);
        }

        /*
         * 
         */
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
                    game.Value.SendMessage(0x05, "Server is shutting down!");
                    game.Value.client.Close();
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
            logger.Log($"Connection Made #{clientsConnected}");
            Game game = null;
            try
            {
                string msg = await ReadMessage(user);
                logger.Log(msg);
                string content = msg.Substring(2);
                if (msg[0] == )
                {
                    logger.Log($"Client reconnected with ID: {msg[1]}");
                    currentGames.TryGetValue(game.clientId, out game);
                }
                else
                {
                    game = new Game(user, "file", GenerateId());
                    currentGames.TryAdd(game.clientId, game);
                    SendMessage(user, 1, game.clientId);
                }
                await Task.Run(() => game.Play(msg));

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

        private string GenerateId()
        {
            return totalUsers.ToString();
        }

        public void SendMessage(TcpClient client, byte messageType, string message)
        {
            var buffer = new byte[message.Length + 1];
            buffer[0] = messageType;
            Encoding.ASCII.GetBytes(message, 0, message.Length, buffer, 1);
            client.GetStream().Write(buffer, 0, buffer.Length);
        }

        public async Task<string> ReadMessage(TcpClient client)
        {
            Byte[] buffer = new Byte[1024];
            NetworkStream stream = client.GetStream();
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
        }

        // TO-DO : Current Task create dynamic start up for server and logger that allows choice of ip, and which dir to get for gameDir,
        // then either create or find file for logDir. But dont nest log dir inside of server, incase you wamt to use it later
    }
}