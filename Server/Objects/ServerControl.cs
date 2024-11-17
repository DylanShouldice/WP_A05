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
    
    public enum Communication
    {
        CONNECTION,
        GAME_MSG,
        NON_GAME,
        SERVER,     // Will be used when the server sends something to the client, when no client input was triggered to do so
                    // I think this might allow us to time out users who spam enter. then maybe we can make their screen red.

    }

    public enum ServerStatus
    {

    }


    public struct Message
    {
        public string content;
        public int client;
        public int type;

        public Message(string msg)
        {
            type = int.Parse(msg[0].ToString());
            if (type != 1)
            {
                client = int.Parse(msg[1].ToString());
            }
            else
            {
                client = 0;
            }
            content = msg.Substring(1);
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


        public struct InitMsg
        {
            public int type;
            public string content;

            public InitMsg(string msg)
            {
                type = int.Parse(msg[0].ToString());
                content = msg.Substring(1);
            }
                

        }

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
            logger.Log("Waiting for clients");

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
                Message msg = new Message(await ReadMessage(user));                
                if (msg.type != 1)
                {
                    logger.Log($"Client reconnected with ID: {msg.client}");
                    currentGames.TryGetValue(game.clientId, out game);
                    await Task.Run(() => game.Play(msg));
                }
                else
                {
                    game = new Game(user, "file", GenerateId());
                    currentGames.TryAdd(game.clientId, game);
                    SendMessage(user, 1, game.clientId);
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
            var buffer = new byte[1024];
            var bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
        }

        // TO-DO : Current Task create dynamic start up for server and logger that allows choice of ip, and which dir to get for gameDir,
        // then either create or find file for logDir. But dont nest log dir inside of server, incase you wamt to use it later
    }
}