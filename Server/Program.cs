using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        static string ChooseIp()
        {
            List<IPAddress> validIps = new List<IPAddress>();
            foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    validIps.Add(ip);
                }
            }

            if (validIps.Count == 0)
            {
                Console.WriteLine("No valid IP addresses found.");
                return string.Empty;
            }

            return validIps[validIps.Count - 1].ToString();
        }



        static async Task Main(string[] args)
        {
            string hostIp = ChooseIp();
            int port = 13000;

            Console.Clear();
            Console.WriteLine(hostIp);
            ServerControl server = new ServerControl(hostIp, port);
            await server.StartServer();
        }
    }
}
