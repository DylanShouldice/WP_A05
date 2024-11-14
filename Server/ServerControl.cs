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
        TcpListener server = null;  // I think I might make a class called Connection that will get instanited each time a user 
                                    // connects to the server, and that is what will be used to serve each user perhaps.
        IPAddress hostIp = null;
        string hostMachine = string.Empty;
        int port = 13000;
        volatile bool keepRunning = true;
        List<Connection> activeUsers = new List<Connection>();
        ConcurrentQueue<TcpClient> connectionQueue = new ConcurrentQueue<TcpClient>();





        // Currently will try to obtain your wireless LAN ipv4 and set it as host machine, but it shows all valid ipv4 on current machine
        // Like in the case of us having VMWare connections, which I think can be used as hosts if we set it up properly in VMWare
        internal void FindHostIP()
        {
            try
            {
                hostMachine = Dns.GetHostName();
                Console.WriteLine(hostMachine);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Caught Socket Exception {Server, 26}", ex);
            }
            var addressList = Dns.GetHostAddresses(hostMachine);
            List<IPAddress> validHostIps = new List<IPAddress>();
            int numValidIp = 1;

            foreach (var address in addressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    validHostIps.Add(address);
                    Console.WriteLine($"#{numValidIp} -- IP Address is : {address.ToString()}");
                    numValidIp++;
                }
            }
            hostIp = validHostIps[numValidIp - 2];
            Console.WriteLine($"Chosen IP Address = {hostIp}");
        }

        internal void Start()
        {
            server = new TcpListener(hostIp, port);
            server.Start();
            Console.WriteLine("Server started running at {0}", DateTime.Now);

            Thread connectionThread = new Thread(ConnectionHandler); // Handles adding clients to queue. I think I will need a main queue that holds all incoming messages so I can get their purpose then create threads for the user as needed
            Task master = new Task(ServerLoop);
            connectionThread.Start();
            master.Start();


        }

        private void ConnectionHandler() // Adds incoming clients into the queue
        {
            while (keepRunning)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Client Added To Queue");

                    connectionQueue.Enqueue(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error accepting client: " + ex.Message);
                }
            }
        }

        public async void ServerLoop() // Checks the queue for incoming clients and accepts them.
        {
            while (keepRunning)
            {
                while(connectionQueue.TryDequeue(out TcpClient client))
                {
                    Connection newConnection = new Connection(client);
                    
                    lock (activeUsers)
                    {
                        Console.WriteLine("Client Connected");
                        activeUsers.Add(newConnection);
                    }
                    // Might need to add something here I have a feeling. idk what though
                }
                await Task.Delay(100);
            }
        }



    }
}





