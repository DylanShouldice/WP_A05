/*
 * FILE             : ServerService.cs
 * PROJECT          : A06 - Services
 * PROGRAMMER       : Oliver Gingerich and Dylan Shouldice-Jacobs
 * FIRST VERSION    : 2024/11/26
 * DESCRIPTION      : This file contains the OnStart and OnStop functions for our service which start and stop our server respectively.
 */
using Server;
using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ServerService
{

    /*
     * =================================================CLASS===============================================|
     * Title    : ServerService                                                                             |
     * Purpose  : The purpose of this class is to manage a service which manages our server. The OnStart    |
     *            function initializes required parts of our server and starts it. OnStop throws the        |
     *            cancellation token which triggers the server shutdown.                                    |
     *======================================================================================================|
     */
    public partial class ServerService : ServiceBase
    {
        ServerControl server;
        public ServerService()
        {
            InitializeComponent();
        }

        /*=====================FUNCTION=================================|
         * Name     : OnStart                                           |
         * Purpose  : To start our server when the service is started.  |
         * Inputs   : String[] args                                     |
         * Outputs  : Logs status messages to a text file.              |
         * Returns  : NONE                                              |
         * =============================================================|
         */
        protected override void OnStart(string[] args)
        {
            Task.Run(() =>
            {
                try
                {
                    Logger.InitalizeLogger();
                    Logger.Log("Starting Server.");

                    string ipAddress = ConfigurationSettings.AppSettings["IP"];
                    int port = int.Parse(ConfigurationSettings.AppSettings["Port"]);
                    Logger.Log($"Initializing ServerControl with IP: {ipAddress}, Port: {port}");
                    server = new ServerControl(ipAddress, port);
                    server.StartServer().Wait();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception occurred while starting the service: {ex.Message}");
                    throw;
                }
            });
        }
        
        /*=====================FUNCTION=================================|
         * Name     : OnStop                                            |
         * Purpose  : To stop our server when the service is stopped.   |
         * Inputs   : String[] args                                     |
         * Outputs  : Logs to log file.                                 |
         * Returns  : NONE                                              |
         * =============================================================|
         */
        protected override void OnStop()
        {
            Logger.Log("Server shutting down.");
            Task.Run(() => server.cts.Cancel());
        }
    }
}
