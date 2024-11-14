using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    enum MessageProtocol // Maybe for use when communicating via client->server
    {
        CONNECTION,
        GAME_RUNNING_REQ,
        NON_GAME_REQ,
    }
    enum ConnectionStatus // States a user can be in
    {
        CONNECTED,
        CONNECTING,
        AWAITING_CONNECTION,
        AWAITING_SERVE,
        IDLE,
        TIMEOUT,

    }

    internal class Connection
    {
        public static int       numActiveUsers { get; set; }
        public int              ClientId { get; set; }
        public ConnectionStatus status { get; set; }
        public TcpClient        client { get; set; } // Client connected
        public Thread           clientThread { get; set; } // Their thread for work
        public NetworkStream    stream { get; set; }       // Stream where clients info can be read and written to.


        public Connection(TcpClient newClient)
        {
            ClientId = ++numActiveUsers;
            status = ConnectionStatus.AWAITING_CONNECTION;

            client = newClient;
            stream = client.GetStream();

            clientThread = new Thread(RequestHandler);
            clientThread.Start(); // Runs the request handler
        }

        internal void RequestHandler()
        {
            try
            {
                status = ConnectionStatus.CONNECTED;
                byte[] msg = StringToTCPMessage("Welcome to the best Server NA");

                stream.Write(msg, 0, msg.Length);
              
                while (status == ConnectionStatus.CONNECTED) // In here is where I will look at the 1st byte and determine what to do
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        string data = System.Text.Encoding.ASCII.GetString(buffer, 0, buffer.Length);
                        Console.WriteLine("Message Recieved");
                        Console.WriteLine("From {0}", ClientId);
                        Console.WriteLine($"Message: {data}");
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        internal byte[] StringToTCPMessage(string text) // Might be useful if I have to write more messages.
        {
            return System.Text.Encoding.ASCII.GetBytes(text);
        }
    }
}
