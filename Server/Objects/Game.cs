using System.Collections.Generic;
using System.Net.Sockets;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.IO;
using System.Linq;
using System.Text;

namespace Server
{
    internal class Game
    {
        public readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly string gameDataDirectory;
        private string currentWordPool = "aaaaaaaaaaaaaaa";
        private int remainingWords = 20;
        private List<string> wordsToGuess;
        public string clientId;

        public Game (TcpClient client, string gameFile, string clientId)
        {
            this.client = client;
            gameDataDirectory = gameFile;
            this.clientId = clientId;
            stream = client.GetStream();
        }

        public struct Message
        {
            public string content;
            public int client;
            public int type;

            public Message(string msg)
            {
                type = int.Parse(msg[0].ToString());
                client = int.Parse(msg[1].ToString());
                content = msg.Substring(1);
            }
        } 

        public async Task Play(CancellationToken cToken) // I think this can be better, I want this function to have only 1 await. 
        {
            try
            {
                while (!cToken.IsCancellationRequested)
                {
                    Message message = new Message(await ReadMessage());
                    Console.WriteLine($"Message Type : {message.type}");
                    Console.WriteLine($"Message content : {message}");

                    switch (message.type)
                    {
                        case 1: 
                            string name = message.content;
                            InitalizeGame();
                            SendMessage(1, $"{currentWordPool}\n{remainingWords}");
                            Console.WriteLine($"Game started at {DateTime.Now}");
                            break;

                        case 2: 
                            CheckGuess(message.content);
                            break;

                        case 3: 
                            SendMessage(3, "Leaving game");
                            return;

                        default:
                            Console.WriteLine("Do not know what to do");
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                SendMessage(4, "Server shutting down");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception caught in 'Play()' {e.Message}");
            }
        }

        private void InitalizeGame() // Sets variables of game object neccesary for the game
        {
            string[] filePool = Directory.GetFiles(gameDataDirectory, "*.txt");
            Random rand = new Random();
            string gameFile = filePool[rand.Next(filePool.Length)];
            string[] gameFileArr = File.ReadAllLines(gameFile);

            currentWordPool = gameFileArr[0];
            remainingWords = int.Parse(gameFileArr[1]);
            wordsToGuess = new List<string>(gameFileArr); // List to hold words, gets removed as guessed
            wordsToGuess.RemoveRange(0, 2);               
        }

        private void CheckGuess(string guess)
        {
            if (wordsToGuess.Contains(guess, StringComparer.OrdinalIgnoreCase))
            {
                wordsToGuess.Remove(guess);
                remainingWords--;
                SendMessage(2, $"Correct! Remaining words: {remainingWords}");
            }
            else
            {
                SendMessage(2, $"Incorrect. Remaining words: {remainingWords}");
            }
        }

        public void SendMessage(byte messageType, string message)
        {
            var buffer = new byte[message.Length + 1];
            buffer[0] = messageType;
            Encoding.ASCII.GetBytes(message, 0, message.Length, buffer, 1);
            stream.Write(buffer, 0, buffer.Length);
        }

        public async Task<string> ReadMessage()
        {
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
        }

    }
}