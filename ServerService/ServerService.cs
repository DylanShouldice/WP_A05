using Server;
using System;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ServerService
{
    public partial class ServerService : ServiceBase
    {
        ServerControl server;
        public ServerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Task.Run(() =>
            {
                try
                {
                    Logger.InitalizeLogger();
                    Logger.Log("Starting Server.");

                    string ipAddress = ServerControl.ChooseIp();
                    int port = 13000;
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

        protected override void OnStop()
        {
            Task.Run(() => server.cts.Cancel());
        }
    }
}
