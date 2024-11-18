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
    //public enum ClientStatus
    //{
    //    CONNECTED,
    //    DISCONNECTED,
    //    AWAITING,
    //    IDLE,
    //    TME_OUT
    //}

    //public struct message
    //{
    //    public string content;
    //    public int client;
    //    public int type;

    //    public message(string msg)
    //    {
    //        type = int.Parse(msg[0].ToString());
    //        client = int.Parse(msg[1].ToString());
    //        content = msg.Substring(1);
    //    }
    //}

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
        //===SENDING CONSTANTS===//
        public const int FIRST_CONNECT  = 1;
        public const int GAME_MSG       = 2;
        //===RECEIVING CONSTANTS===//
        public const int GAMEINFO   = 1;    //Message has string & num of words
        public const int PLAYAGAIN  = 2;    //User won or time is up - prompt play again
        public const int SERVERDOWN = 3;    //Server shut down - End game

        private NetworkStream stream;
        private TcpClient client;

        public int gameID;
        public string chars;
        public string numWords;
        public bool playAgain = false;
        public bool serverdown = false;


        /*===========================================FUNCTION===========================================|
         * Name     : ConnectClient                                                                     |
         * Purpose  : receive messages from the server/UI levels and send them to the correct places.   |
         * Inputs   : String server - IP address     String message      Int32 port                     |
         * Outputs  : NONE                                                                              |
         * Returns  : NONE                                                                              |
         * =============================================================================================|
         */
        public async Task ConnectClient(String server, String message, Int32 port)
        {
            string serverResponse = string.Empty;
            try
            {
                //==SENDING/RETREIVING DATA===//
                client = new TcpClient(server, port);
                stream = client.GetStream();

                SendMessage(client, message);
                serverResponse = await ReadMessage(client);
                
                //===UNDERSTANDING DATA RETREIVED===//
                string[] parsed = serverResponse.Split(' ');
                int.TryParse(parsed[0], out int indicator); //Parse indicator message

                switch (indicator)
                {
                    case GAMEINFO:
                        int.TryParse(parsed[1], out gameID);    //Parse game ID to send to server in future
                        chars = parsed[2];  //String of characters
                        numWords = parsed[3];
                        break;
                    case PLAYAGAIN:
                        playAgain = true;   //Indicates to UI layer that play again screen needs to appear
                        break;
                    case SERVERDOWN:
                        serverdown = true;
                        break;
                }

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
