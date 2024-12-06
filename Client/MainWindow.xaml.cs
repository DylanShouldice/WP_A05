﻿/*
 * FILE             : MainWindow.xaml.cs
 * PROJECT          : A05 - InterProcessCommunication
 * PROGRAMMER       : Oliver Gingerich
 * FIRST VERSION    : 2024/11/13
 * DESCRIPTION      : This file contains the UI level functionality (i.e button clicks, input validation)
 *                    before using class level functions to send information to the server.
 */
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Client
{
    public partial class MainWindow : Window
    {
        //===SENDING CONSTANTS===//
        public const string FIRST_CONNECT = "1";
        public const string GAME_MSG = "2";
        public const string EXITING_GAME = "3";
        public const string TIME_UP = "4";
        public const string PLAY_AGAIN = "5";
        public const string EXIT = "6";
        //===SENDING CONSTANTS - IN THE CASE OF EXIT CONFIRM===//
        public const string YES = "0";
        public const string NO = "1";
        //===RECEIVING CONSTANTS===//
        public const string GAMEINFO = "1";
        public const string WIN = "2";
        public const string SERVERDOWN = "3";
        //===OTHER CONSTANTS===//
        public const int PORT = 13000;

        private Client_End client;
        TimeSpan time;
        DispatcherTimer dpt;


        public MainWindow()
        {
            InitializeComponent();
            client = new Client_End();

            //Initializing time to be used later
            dpt = new DispatcherTimer();
            dpt.Interval = TimeSpan.FromSeconds(1);
            dpt.Tick += Timer_Tick; //Ticks timer on interval
        }

        /*
         * ===========================================FUNCTION==========================================|
         * Name     : start_btn_Click                                                                   |
         * Purpose  : To validate input then send it to the server. Upon valid input, settings panel    |
         *            should be disabled and game input should be enabled.                              |
         * Inputs   : object sender     RoutedEventArgs e                                               |
         * Outputs  : Updates UI to allow for game input                                                |
         * Returns  : NONE                                                                              |
         * =============================================================================================|
         */
        private async void start_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate_Input())
            {
                return; //Leave function if values are not valid
            }

            string server = IP_txt.Text;    //IP address of server
            string message = FIRST_CONNECT + " " + Name_txt.Text; //To send to the server
            Int32 port;
            int.TryParse(Port_txt.Text, out port);  //Parse and assign the port

            await client.ConnectClient(server, message, port);    //Send message and get info
            if (client.client != null)
            {
                //IF ALL INPUT IS VALID -> SWAP UI
                Game_Cover.Visibility = Visibility.Hidden;
                Input_Cover.Visibility = Visibility.Visible;

                //Updating UI to reflect string and current words remaining
                String_txt.Text = client.chars;
                NumWords_txt.Text = client.numWords;

                ConnectError.Visibility = Visibility.Hidden;    //Hide connection error incase it was triggered previously

                Start_Timer();
            }
            else
            {
                ConnectError.Visibility = Visibility.Visible;
            }
        }

        /*
        * ===================FUNCTION==============================|
        * Name     : Start_Timer                                   |
        * Purpose  : To start the Timer for a game.                |
        * Inputs   : NONE                                          |
        * Outputs  : NONE                                          |
        * Returns  : NONE                                          |
        * =========================================================|
        */
        public void Start_Timer()
        {
            dpt = new DispatcherTimer();
            time = TimeSpan.FromMinutes(client.timeLimit);  //Sets time limit
            dpt.Interval = TimeSpan.FromSeconds(1);
            dpt.Tick += Timer_Tick; //Ticks timer on interval
            dpt.Start();
        }

        /*
        * ===================FUNCTION==============================|
        * Name     : Timer_Tick                                    |
        * Purpose  : To tick the timer                             |
        * Inputs   : object sender      EventArgs e                |
        * Outputs  : Updates the timer on the UI level             |
        * Returns  : NONE                                          |
        * =========================================================|
        */
        public async void Timer_Tick(object sender, EventArgs e)
        {
            if (time == TimeSpan.Zero && Game_Cover.Visibility == Visibility.Hidden)  //If out of time and in game
            {
                client.timeUp = true;
                dpt.Stop();
                string server = IP_txt.Text;
                string message = $"{TIME_UP} {client.gameID}";
                await client.ConnectClient(server, message, PORT);
                if (client.playAgain)
                {
                    await restart();
                }
            }
            else
            {
                time = time.Add(TimeSpan.FromSeconds(-1));
                //Update timer in UI
                client.timeUp = false;
                gameTimer.Content = time.ToString("c");
            }
        }

        /*
         * ===========================================FUNCTION==========================================|
         * Name     : Guess_btn_Click                                                                   |
         * Purpose  : To validate input then send it to the server. Upon valid input, number of words   |
         *            should be updated (or client informed server is shutting down).                   |
         * Inputs   : object sender     RoutedEventArgs e                                               |
         * Outputs  : Updates UI to show remaining words or inform that server is shutting down         |
         * Returns  : NONE                                                                              |
         * =============================================================================================|
         */
        private async void Guess_btn_Click(object sender, RoutedEventArgs e)
        {
            //VALIDATE CONTENTS OF FIELDS HERE
            //Alphabetical letters only
            //Not empty
            if (String.IsNullOrEmpty(Guess_txt.Text.Trim()))
            {
                guessError.Content = "Guess may not be empty.";
            }
            else if (!Regex.IsMatch(Name_txt.Text, @"^[a-zA-Z]+$"))
            {
                guessError.Content = "Guess must be a letter.";
            }
            else
            {
                guessError.Content = string.Empty;
            }

            //PLACEHOLDER FOR TESTING - Will be same information as was sent in start_btn_Click (Other than message)
            //PLACEHOLDER FOR TESTING
            string server = IP_txt.Text;
            string message = GAME_MSG + " " + client.gameID + " " + Guess_txt.Text.ToLower(); //Combine indicator with the guess
            Int32 port;
            if (int.TryParse(Port_txt.Text, out port)) //Parse and assign the port
            {
                await client.ConnectClient(server, message, port);

                if (client.playAgain == true)
                {
                    if (!await restart())
                    {
                        Game_Cover.Visibility = Visibility.Visible;
                        Input_Cover.Visibility = Visibility.Hidden;
                    }
                }
                else if (client.serverdown)
                {
                    Game_Cover.Visibility = Visibility.Visible;
                    Input_Cover.Visibility = Visibility.Hidden;
                    MessageBoxResult result = MessageBox.Show("Server is shutting down", "Server Closing", MessageBoxButton.OK, MessageBoxImage.Question);
                    ResetClientState();
                    //client.SendMessage(client.client, "ree");
                }
                else
                {
                    NumWords_txt.Text = client.numWords;
                }
            }
        }


        /*
        * ===================FUNCTION==============================|
        * Name     : ResetClientState                              |
        * Purpose  : Reset the users view of the client            |
        * Inputs   : NONE                                          |
        * Outputs  : Displays a message box asking about restart.  |
        * Returns  : NONE                                          |
        * =========================================================|
        */
        public void ResetClientState()
        {
            // Reset UI elements
            Game_Cover.Visibility = Visibility.Visible;
            Input_Cover.Visibility = Visibility.Hidden;
            Guess_txt.Text = string.Empty;
            NumWords_txt.Text = string.Empty;
            String_txt.Text = string.Empty;
            gameTimer.Content = "0:00";
            Guess_txt.Text = "";
            guessError.Content = "";
            client.timeLimit = 0;
            client.chars = "";
            client.numWords = "";
            client.serverdown = false;
            client.playAgain = false;
            client.timeUp = false;

            dpt.Stop();
        }


        /*
        * ===================FUNCTION==============================|
        * Name     : restart                                       |
        * Purpose  : To ask the user if they want to play again.   |
        * Inputs   : NONE                                          |
        * Outputs  : Displays a message box asking about restart.  |
        * Returns  : NONE                                          |
        * =========================================================|
        */
        private async Task<bool> restart()
        {
            dpt.Stop();
            string msg = string.Empty;
            bool restart = false;

            if (client.timeUp == true)
            {
                msg = "You ran out of time. Play again?";
            }
            else
            {
                msg = "You win, congrats! Play again?";
            }

            MessageBoxResult result = MessageBox.Show(msg, "Play Again?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    msg = $"{PLAY_AGAIN} {client.gameID}";
                    restart = true;
                    break;
                case MessageBoxResult.No:
                    msg = $"{EXIT} {client.gameID}";
                    ResetClientState(); // my attempt at reseting the client side. 
                    restart = false;
                    break;
            }

            //===Reset UI and Talk to Server===//
            await client.ConnectClient(IP_txt.Text, msg, PORT);
            String_txt.Text = client.chars;
            NumWords_txt.Text = client.numWords;
            //===Restart Timer===//
            if (restart == true)
            {
                time = TimeSpan.FromMinutes(client.timeLimit);  //Sets time limit
                dpt.Start();
            }
            else
            {
                dpt.Stop();
            }
            //===Reset State Bools===//
            client.playAgain = false;
            Guess_txt.Text = string.Empty;
            return restart;
        }

        /*
         * ===================FUNCTION==============================|
         * Name     : Validate_Input                                |
         * Purpose  : To validate user input                        |
         * Inputs   : NONE                                          |
         * Outputs  : Displays error messages upon invalid input.   |
         * Returns  : A bool indicating if input was valid.         |
         * =========================================================|
         */
        private bool Validate_Input()
        {
            //===NAME VALIDATION===//
            if (String.IsNullOrEmpty(Name_txt.Text) || String.IsNullOrWhiteSpace(Name_txt.Text))
            {
                NameError.Content = "Must input a name.";
                return false;
            }
            else if (!Regex.IsMatch(Name_txt.Text, @"^[a-zA-Z]+$"))
            {
                NameError.Content = "Must only be letters.";
                return false;
            }
            else
            {
                NameError.Content = String.Empty;
            }

            //===VALIDATING TIME LIMIT===//
            if (String.IsNullOrEmpty(TimeLimit_txt.Text) || String.IsNullOrWhiteSpace(TimeLimit_txt.Text))
            {
                TimeError.Content = "Must input a limit.";
                return false;
            }
            else if (!int.TryParse(TimeLimit_txt.Text, out client.timeLimit))
            {
                TimeError.Content = "Must be an integer.";
                return false;
            }
            else if (client.timeLimit < 1)
            {
                TimeError.Content = "Must be larger than 0.";
                return false;
            }
            else
            {
                TimeError.Content = String.Empty;
            }

            //===VALIDATING THE IP===//
            if (String.IsNullOrEmpty(IP_txt.Text) || String.IsNullOrWhiteSpace(IP_txt.Text))
            {
                IPError.Content = "Must input an IP.";
                return false;
            }
            if (!IPAddress.TryParse(IP_txt.Text, out IPAddress IP))
            {
                IPError.Content = "Input must be an IP.";
                return false;
            }
            else
            {
                IPError.Content = String.Empty;
            }

            //===VALIDATING THE PORT===//
            if (String.IsNullOrEmpty(Port_txt.Text.Trim()))
            {
                PortError.Content = "Must input a port.";
                return false;
            }
            else if (!int.TryParse(Port_txt.Text, out int port))
            {
                PortError.Content = "Must be an integer.";
                return false;
            }
            else
            {
                PortError.Content = String.Empty;
            }

            return true;
        }


        /*
         * ======================================FUNCTION==============================|
         * Name     : Window_Closing                                                    |
         * Purpose  : Confirm if user actually wishes to quit while game is in session. |
         * Inputs   : object sender     System.ComponentModel.CancelEventArgs e         |
         * Outputs  : Displays a message box asking if user wishes to exit.             |
         * Returns  : NONE                                                              |
         * =============================================================================|
         */
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Game_Cover.Visibility == Visibility.Hidden && !client.close)
            {
                e.Cancel = true;
                string server = IP_txt.Text;    //IP address of server
                string message = $"{EXITING_GAME} {client.gameID}"; //To send to the server
                Int32 port;
                int.TryParse(Port_txt.Text, out port);  //Parse and assign the port

                await client.ConnectClient(server, message, port);    //Send message and get info
                MessageBoxResult result = MessageBox.Show("Game in progress; Are you sure you want to exit?", "Game in Progress", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        await client.ConfirmClose(server, EXIT, port);
                        client.close = true;
                        this.Close();
                        break;
                    case MessageBoxResult.No:
                        await client.ConfirmClose(server, 0.ToString(), port);
                        e.Cancel = true;
                        break;
                }
            }
        }
    }
}
