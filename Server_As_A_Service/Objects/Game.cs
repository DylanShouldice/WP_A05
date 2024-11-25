using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;


namespace Server_As_A_Service
{
    internal class Game
    {
        public TcpClient client;
        public readonly string gameDataDirectory;
        public string currentWordPool;
        public int remainingWords;
        public List<string> wordsToGuess;
        public int clientId;
        public string clientName;
        public bool close;


        public Game(TcpClient user, string gameFile, int clientId)
        {
            this.client = user;
            gameDataDirectory = gameFile;
            this.clientId = clientId;
        }

        public string Play(string[] msg) // I think this can be better, I want this function to have only 1 await. 
        {
            try
            {
                CheckGuess(msg[2]);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception caught in 'Play()' {e.Message}");
            }
            return remainingWords.ToString();
        }

        public void InitalizeGame() // Sets variables of game object neccesary for the game
        {
            string[] filePool = Directory.GetFiles(gameDataDirectory, "*.txt");
            Random rand = new Random();
            string gameFile = filePool[rand.Next(filePool.Length)];    //"gameDir\\test.txt";
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
            }
            else
            {

            }
        }


    }
}