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
    internal class Client_End
    {
        private void ConnectClient(String server, String message)
        {
            try
            {
                Int32 port = 13000;
                TcpClient client = new TcpClient(server, port); //Creates TcpClient

                //Convert information into byte data to send to Server here

                NetworkStream stream = client.GetStream();  //Creating a stream

                //Send data to server here

                Byte[] data = new Byte[256];    //To store server response (game information)
                String response = String.Empty; //String to store server response

                //Convert response & process it as needed here

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
        }
    }
}
