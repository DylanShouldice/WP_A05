/*
 * FILE             : Client_End.cs
 * PROJECT          : A05 - InterProcessCommunication
 * PROGRAMMER       : Oliver Gingerich
 * FIRST VERSION    : 2024/11/15
 * DESCRIPTION      : This file contains the Client_End class which should handle all connections between
 *                    the UI and the server end.
 */
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
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
        public const int FIRST_CONNECT = 1;
        public const int GAME_MSG = 2;
        public const int EXITING_GAME = 3;
        public const int TIME_UP = 4;
        public const int PLAY_AGAIN = 5;
        public const int DELETE_GAME = 6;
        //===SENDING CONSTANTS - IN THE CASE OF EXIT CONFIRM===//
        public const int YES = 0;
        public const int NO = 1;
        //===RECEIVING CONSTANTS===//
        public const int GAME_INFO = 1;    //Message has string & num of words
        public const int WORD_COUNT = 2;
        public const int SERVER_DOWN = 3;    //Server shut down
        public const int REPLAY_PROMPT = 4;    //User won or time is up - prompt play again
        public const int EXIT_CONFIRM = 5;

        private NetworkStream stream;
        public TcpClient client;

        public int gameID;
        public int timeLimit;
        public string chars;
        public string numWords;
        public string server;
        public bool playAgain = false;
        public bool serverdown = false;
        public bool exitConfirm = false;
        public bool timeUp = false;
        public bool close = false;


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
                    case GAME_INFO:
                        int.TryParse(parsed[1], out gameID);    //Parse game ID to send to server in future
                        chars = parsed[2];  //String of characters
                        numWords = parsed[3];
                        break;
                    case WORD_COUNT:
                        numWords = parsed[2];
                        break;
                    case REPLAY_PROMPT:
                        playAgain = true;   //Indicates to UI layer that play again screen needs to appear
                        break;
                    case EXIT_CONFIRM:
                        exitConfirm = true;
                        break;
                    case SERVER_DOWN:
                        serverdown = true;
                        break;
                    default:
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

        /*======================FUNCTION================|
         * Name     : SendMessage                       |
         * Purpose  : To send a message to the server.  |
         * Inputs   : TcpClient client  string message  |
         * Outputs  : NONE                              |
         * Returns  : NONE                              |
         * =============================================|
         */
        public void SendMessage(TcpClient client, string message)
        {
            var buffer = Encoding.ASCII.GetBytes(message);
            client.GetStream().Write(buffer, 0, buffer.Length);
        }

        /*======================FUNCTION================|
         * Name     : ReadMessage                       |
         * Purpose  : To read a message from the server.|
         * Inputs   : TcpClient client  string message  |
         * Outputs  : NONE                              |
         * Returns  : Task<string> - Message read       |
         * =============================================|
         */
        public async Task<string> ReadMessage(TcpClient client)
        {
            Byte[] buffer = new Byte[1024];
            int bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
        }

        /*======================FUNCTION============================|
         * Name     : ConfirmClose                                  |
         * Purpose  : To confirm game closing to server.            |
         * Inputs   : string server     string yesNo    Int32 port  |
         * Outputs  : NONE                                          |
         * Returns  : NONE                                          |
         * =========================================================|
         */
        public async Task<string> ConfirmClose(string server, string yesNo, Int32 port)
        {
            //==SENDING/RETREIVING DATA===//
            client = new TcpClient(server, port);
            stream = client.GetStream();
            string message = $"{yesNo} {gameID}";

            SendMessage(client, message);
            await ReadMessage(client);
            return yesNo; //For the sake of returning
        }
    }
}
