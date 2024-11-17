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
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly string gameDataDirectory;
        private string currentWordPool = "aaaaaaaaaaaaaaa";
        private int remainingWords = 20;
        private List<string> wordsToGuess;
        private string clientId;

        public Game (TcpClient client, string gameFile, string clientId)
        {
            this.client = client;
            gameDataDirectory = gameFile;
            this.clientId = clientId;
            stream = client.GetStream();
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

        public async Task Play(CancellationToken cToken) // I think this can be better, I want this function to have only 1 await. 
        {
            try
            {
                while(!cToken.IsCancellationRequested)
                {
                    message message = new message(await ReadMessage());
                    Console.WriteLine($"Message Type : {message.type}");
                    Console.WriteLine($"Message content : {message}");

                    if (message.type == 1)
                    {
                        string name = content;

                       // InitalizeGame();

                        SendMessage(0x01, $"{currentWordPool}\n{remainingWords}");
                        Console.WriteLine($"Game started at {DateTime.Now}");

                        while (remainingWords > 0 && !cToken.IsCancellationRequested)
                        {
                            message = await ReadMessage();
                            messageType = int.Parse(message.Substring(0, 1));
                            content = message.Substring(1);
                            Console.WriteLine($"Message Type : {messageType}");
                            Console.WriteLine($"Message content : {message}");

                            if (messageType == 2)
                            {
                                //CheckGuess(content); // This should send a message that signifies the guess
                            }
                            else if (messageType == 3)
                            {
                                SendMessage(3, "Leaving game");
                                // await message here and get a yes / no
                                return;
                            }
                            else
                            {
                                Console.WriteLine("Do not know what to do");
                            }
                        }
                        if (remainingWords == 0)
                        {
                            SendMessage(4, "Win");
                        }
                        else
                        {
                            SendMessage(4, "No more time sorry!");
                        }

                        // check for message if user wants to play again, then go through game loop again
                        // otherwise say goodbye to user

                    }
                    else
                    {
                        Console.WriteLine("Invalid message, client tried to send game message without connection"); // If somehow the client sends a message with a code relating to a game, but their game is not yet active
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