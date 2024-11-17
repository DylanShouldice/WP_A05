/*
 * FILE             : Client_End.cs
 * PROJECT          : A05 - InterProcessCommunication
 * PROGRAMMER       : Oliver Gingerich
 * FIRST VERSION    : 2024/11/15
 * DESCRIPTION      : This file contains the Client_End class which should handle all connections between
 *                    the UI and the server end.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Diagnostics;

namespace Client
{
    public enum ClientStatus
    {
        CONNECTED,
        DISCONNECTED,
        AWAITING,
        IDLE,
        TME_OUT
    }

    public struct message
    {
        public string content;
        public int client;
        public int type;

        public message(string msg)
        {
            type = int.Parse(msg[0].ToString());
            client = int.Parse(msg[1].ToString());
            content = msg.Substring(1);
        }
    }

    /*
     * =================================================CLASS================================================|
     * Title    : Client_End                                                                                |
     * Purpose  : The purpose of this class is to serve as the connection between the UI and server end     |
     *            levels of the project. It should recieve messages from the UI level and send it to the    |
     *            server. It should also receive messages from the server and send them to the correct      |
     *            places within the UI level.                                                               |
     *======================================================================================================|
     */
    internal class Client_End
    {

        private NetworkStream stream;

        /*===========================================FUNCTION===========================================|
         * Name     : ConnectClient                                                                     |
         * Purpose  : receive messages from the server/UI levels and send them to the correct places.   |
         * Inputs   : String server - IP address     String message      Int32 port                     |
         * Outputs  : NONE                                                                              |
         * Returns  : Message msg - Message to give to UI level                                         |
         * =============================================================================================|
         */
        public async Task <string> ConnectClient(String server, String message, Int32 port, Byte indicator)
        {
            string serverResponse = string.Empty;
            try
            {
                TcpClient client = new TcpClient(server, port); //Creates TcpClient

                NetworkStream stream = client.GetStream();  //Creating a stream

                SendMessage(client, "1Start Game");
                //Sending message to stream
                //Byte[] buffer = new Byte[message.Length + 1];
                //buffer[0] = indicator;
                //Encoding.ASCII.GetBytes(message, 0, message.Length, buffer, 1);
                //stream.Write(buffer, 0, buffer.Length);

                serverResponse = await ReadMessage(client);
               
                //DO STUFF WITH DATA HERE

                //Close everything
                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException ex)
            {
                Trace.WriteLine("ArgumentNullException: {0}", ex.ToString());
            }
            catch (SocketException ex)
            {
                Trace.WriteLine("SocketException: {0}", ex.ToString());
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Exception Caught in : 'ConnectClient()' {e}");
            }
            
            return serverResponse;
        }

        public void SendMessage(TcpClient client, string message)
        {
            var buffer = new byte[message.Length + 1];
            Encoding.ASCII.GetBytes(message, 0, message.Length, buffer, 1);
            client.GetStream().Write(buffer, 0, buffer.Length);
        }

        public async Task<string> ReadMessage(TcpClient client)
        {
            Byte[] buffer = new Byte[1024];
            int bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
        }
    }
}
