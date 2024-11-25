using Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;

namespace Server_As_A_Service
{
    internal static class Program
    {
        static string GetIp()
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

            string chosenIp = validIps[validIps.Count].ToString();
            return chosenIp;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServerControl(GetIp(), 13000),
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
