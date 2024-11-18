using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
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

            string chosenIp = ChooseChoice(validIps);
            return chosenIp;
        }

        static string ChooseChoice<T>(List<T> options)
        {
            bool chosen = false;
            int selectedIndex = 0;
            string choice = string.Empty;
            int len = options.Count - 1; // Fix: Use Count instead of Length

            while (!chosen)
            {
                Console.Clear();
                Console.WriteLine("Backspace: Quit -- Enter: Select");
                for (int i = 0; i < options.Count; i++) // Fix: Use Count instead of Length
                {
                    if (selectedIndex == i)
                    {
                        Console.WriteLine($"$ {options[i].ToString()}");
                    }
                    else
                    {
                        Console.WriteLine($"  {options[i].ToString()}");
                    }
                }
                ConsoleKeyInfo key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (selectedIndex != 0)
                        {
                            selectedIndex--;
                        }
                        else
                        {
                            selectedIndex = len;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if (selectedIndex != len)
                        {
                            selectedIndex++;
                        }
                        else
                        {
                            selectedIndex = 0;
                        }
                        break;
                    case ConsoleKey.Enter:
                        return options[selectedIndex].ToString();
                    case ConsoleKey.Backspace:
                        Console.WriteLine("Exiting");
                        return string.Empty;
                    default:
                        Console.WriteLine();
                        break;
                }
            }
            return choice;
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
