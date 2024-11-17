﻿using System;
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
        
    internal class ServerControl
    {
        private readonly TcpListener listener;
        private readonly ConcurrentDictionary<string, Game> currentGames = new ConcurrentDictionary<string, Game>();
        private readonly ConcurrentQueue<TcpClient> connectionQueue = new ConcurrentQueue<TcpClient>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly Logger logger;
        private readonly string gameDir;
        private int clientsConnected;
        private int activeUsers;
        
        
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
                    game.Key.Close();
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
            clientsConnected++;

            try
            {
                string clientId = await ReadMessage();
                Game game;
                
                if (!string.IsNullOrEmpty(clientId) && currentGames.TryGetValue(clientId, out game))
                {
                    logger.Log($"Client reconnected with ID: {clientId}");
                }
                else
                {
                    logger.Log($"New connection wtih ID: {clientId}");
                    clientId = GenerateId(); 
                }
                
            }
            catch (Exception e)
            {
                logger.Log($"Exception caught -- ServerControl.HandleClient() -- {e.Message}");
            }
            finally
            {
                currentGames.TryRemove(user, out _);
                user.Close();
                clientsConnected--;
                logger.Log($"Client : {user.Client.RemoteEndPoint} has disconnected");
            }
        }

        public void SendMessage(byte messageType, string message)
        {
            var buffer = new byte[message.Length + 1];
            buffer[0] = messageType;
            Encoding.ASCII.GetBytes(message, 0, message.Length, buffer, 1);
            stream.Write(buffer, 0, buffer.Length);
        }

        public async Task<string> ReadMessage()
        {
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
        }

        private int GenerateId(string toBeHashed)
        {
            
        }

        // TO-DO : Current Task create dynamic start up for server and logger that allows choice of ip, and which dir to get for gameDir,
        // then either create or find file for logDir. But dont nest log dir inside of server, incase you wamt to use it later
    }
}