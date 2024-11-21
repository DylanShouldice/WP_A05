/*
 * Author : Dylan Shouldice-Jacobs
 * Purpose: The purpose of this class was to remove the game logic from the server class.
 *          Allowing for modularity and easy changes if any game rules needed to be changed
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Server
{
    internal class Game
    {
        public readonly string gameDataDirectory;
        public string currentWordPool;
        public int remainingWords;
        public string prevWordPool;
        public List<string> wordsToGuess;
        public int clientId;
        public string clientName;
        public bool close;


        public Game(string gameFile, int clientId)
        {
            gameDataDirectory = gameFile;
            this.clientId = clientId;
        }
        /*
        *  Input   : msg - the guess
        *  Process : calls check guess
        *  Output  : returns number of words left
        */
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
        /*
        *  Input   : NONE
        *  Process : sets up a new game
        *  Output  : NONE
        */
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
        /*
        *  Input   : guess
        *  Process : check if guess is in the remaining word pool
        *  Output  : NONE
        */
        private void CheckGuess(string guess)
        {
            if (wordsToGuess.Contains(guess, StringComparer.OrdinalIgnoreCase))
            {
                wordsToGuess.Remove(guess);
                remainingWords--;
            }
        }


    }
}