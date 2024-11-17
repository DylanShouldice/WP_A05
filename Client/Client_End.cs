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
    public struct Message
    { 
        string content {  get; set; }
        int type { get; set; }
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
        public Message ConnectClient(String server, String message, Int32 port)
        {
            try
            {
                TcpClient client = new TcpClient(server, port); //Creates TcpClient

                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message); //Taken from example - may need tweaking

                NetworkStream stream = client.GetStream();  //Creating a stream

                stream.Write(data, 0, data.Length); //Taken from example - may need tweaking

                data = new Byte[256];    //To store server response (game information)
                String response = String.Empty; //String to store server response

                //Taken from example - may need tweaking
                Int32 bytes = stream.Read(data, 0, data.Length);
                response = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //DO STUFF WITH DATA HERE

                //Close everything
                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("ArgumentNullException: {0}", ex);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: {0}", ex);
            }

            //NOT WORKING HERE TO MAKE COMPILER HAPPY
            Message msg = new Message();
            return msg;
        }
    }
}
