using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string serverIp = "10.0.0.34";
            int port = 13000;

            try
            {
                using (TcpClient client = new TcpClient(serverIp, port))
                using (NetworkStream stream = client.GetStream())
                {
                    Console.WriteLine("Connected to the server.");
                    Task readTask = Task.Run(() => ReadMessages(stream));

                    while (true)
                    {
                        string message = Console.ReadLine();
                        if (string.IsNullOrEmpty(message))
                        {
                            continue;
                        }
                        byte[] data = Encoding.ASCII.GetBytes(message);
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }

        static async void ReadMessages(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("Server: " + response);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                    break;
                }
            }
        }
    }
}