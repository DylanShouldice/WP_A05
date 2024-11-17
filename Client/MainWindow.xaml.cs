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
        private void start_btn_Click(object sender, RoutedEventArgs e)
        {
            //VALIDATE CONTENTS OF FIELDS HERE//
            //Not empty
            //Correct data type

            //PLACEHOLDER FOR TESTING
            string server = IP_txt.Text;
            string message = "Start Game";  //will be the identifier sent to server?
            Int32 port;
            int.TryParse(Port_txt.Text, out port);  //Parse and assign the port

            message msg = client.ConnectClient(server, message, port, 1);    //Send message and get info
            
            //DO STUFF WITH MESSAGE HERE
            // if or switch statement?
            //Should realistically only have to change UI here - Not sure what other msg would be sent

            //IF ALL INPUT IS VALID -> SWAP UI
            Game_Cover.Visibility = Visibility.Hidden;
            Input_Cover.Visibility = Visibility.Visible;
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
        private void Guess_btn_Click(object sender, RoutedEventArgs e)
        {
            //VALIDATE CONTENTS OF FIELDS HERE
            //Alphabetical letters only
            //Not empty

            //PLACEHOLDER FOR TESTING - Will be same information as was sent in start_btn_Click (Other than message)
            //PLACEHOLDER FOR TESTING
            string server = IP_txt.Text;
            string message = "Word";  //will be the identifier sent to server?
            Int32 port;
            int.TryParse(Port_txt.Text, out port);  //Parse and assign the port

            message msg = client.ConnectClient(server, message, port, 2);

            //DO STUFF WITH MESSAGE HERE//
            //Realistically should only update number of guesses left
            //Or tell us server shut down :(
            //Or tell us we win! :D
        }
    }
}
