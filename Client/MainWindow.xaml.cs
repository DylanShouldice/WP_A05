/*
 * FILE             : MainWindow.xaml.cs
 * PROJECT          : A05 - InterProcessCommunication
 * PROGRAMMER       : Oliver Gingerich
 * FIRST VERSION    : 2024/11/13
 * DESCRIPTION      : This file contains the UI level functionality (i.e button clicks, input validation)
 *                    before using class level functions to send information to the server.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
    public partial class MainWindow : Window
    {
        //===SENDING CONSTANTS===//
        public const string FIRST_CONNECT = "1";
        public const string GAME_MSG = "2";
        public const string EXITING_GAME = "3";
        //===SENDING CONSTANTS - IN THE CASE OF EXIT CONFIRM===//
        public const string YES = "0";
        public const string NO = "1";
        //===RECEIVING CONSTANTS===//
        public const string GAMEINFO = "1";
        public const string WIN = "2";
        public const string SERVERDOWN = "3";

        private Client_End client;

        public MainWindow()
        {
            InitializeComponent();
            client = new Client_End();
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
            string message = FIRST_CONNECT; //To send to the server
            Int32 port;
            int.TryParse(Port_txt.Text, out port);  //Parse and assign the port

            await client.ConnectClient(server, message, port);    //Send message and get info

            //IF ALL INPUT IS VALID -> SWAP UI
            Game_Cover.Visibility = Visibility.Hidden;
            Input_Cover.Visibility = Visibility.Visible;

            //Updating UI to reflect string and current words remaining
            String_txt.Text = client.chars;
            NumWords_txt.Text = client.numWords;
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

            //PLACEHOLDER FOR TESTING - Will be same information as was sent in start_btn_Click (Other than message)
            //PLACEHOLDER FOR TESTING
            string server = IP_txt.Text;
            string message = GAME_MSG + " " + client.gameID + " " + Guess_txt.Text; //Combine indicator with the guess
            Int32 port;
            if (int.TryParse(Port_txt.Text, out port)) //Parse and assign the port
            {
                await client.ConnectClient(server, message, port);
            }

            //DO STUFF WITH MESSAGE HERE//
            //Realistically should only update number of guesses left
            //Or tell us server shut down :(
            //Or tell us we win! :D
        }

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

            return true;
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string server = IP_txt.Text;    //IP address of server
            string message = $"{EXITING_GAME} {client.gameID}"; //To send to the server
            Int32 port;
            int.TryParse(Port_txt.Text, out port);  //Parse and assign the port

            await client.ConnectClient(server, message, port);    //Send message and get info

            if (client.exitConfirm)
            {
               MessageBoxResult result = MessageBox.Show("Game in progress; Are you sure you want to exit?", "Game in Progress", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        await client.ConfirmClose(server, YES, port);
                        break;
                    case MessageBoxResult.No:
                        await client.ConfirmClose(server, NO, port);
                        e.Cancel = true;
                        break;
                }
            }
        }
    }
}
