using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;

namespace Server
{
    internal class ServerControl
    {
        private readonly TcpListener listener;
        private readonly ConcurrentDictionary<TcpClient, Game> currentGames = new ConcurrentDictionary<TcpClient, Game>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly string gameDir;
        private int clientsConnected;
        private int activeUsers;


        public ServerControl(string ip, int port, string gameDir)
        {
            this.gameDir = gameDir;
            listener = new TcpListener(IPAddress.Parse(ip), port);
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
            Console.WriteLine($"--- Server Started at {DateTime.Now} ---");
            Console.WriteLine("Waiting for clients");

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = HandleClient(client, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception caught in 'StartServer()' : {e.Message}");
                }
            }
        }

        public void StopServer()
        {
            Console.WriteLine($"Server stop started at {DateTime.Now}");
            cts.Cancel();
            listener.Stop(); 

            foreach (var game in currentGames)
            {
                try
                {
                    game.Value.SendMessage(0x05, "Server is shutting down!");
                    game.Key.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception caught in 'StopServer()' : {e.Message}");
                }
            }

            currentGames.Clear();
            Console.WriteLine($"Server stopped at {DateTime.Now}");
        }

        private async Task HandleClient(TcpClient user, CancellationToken cToken)
        {
            clientsConnected++;
            Console.WriteLine($"Connection Made #{clientsConnected}");

            try
            {
                Game game = new Game(user, gameDir);
                currentGames.TryAdd(user, game);
                await game.Play(cToken);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception Caught in 'HandleClient()' : {e.Message}");
            }
            finally
            {
                currentGames.TryRemove(user, out _);
                user.Close();
                Console.WriteLine($"Client : {user.Client.RemoteEndPoint} has disconnected");
            }
        }
    }
}