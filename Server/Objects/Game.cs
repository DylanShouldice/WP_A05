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
        public readonly NetworkStream stream;
        public readonly string gameDataDirectory;
        public string currentWordPool = "aaaaaaaaaaaaaaa";
        public int remainingWords = 20;
        public List<string> wordsToGuess;
        public int clientId;
        public string clientName;


        public Game(string gameFile, int clientId)
        {
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
            }
            else
            {

            }
        }


    }
}