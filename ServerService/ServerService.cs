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
            try
            {
                Logger.InitalizeLogger();
                server = new ServerControl(ServerControl.ChooseIp(), 13000);
                Task.Run(() => server.StartServer());
            }
            catch (Exception e)
            {
                Logger.Log($"Exception Caught -- {e}");
            }


        }

        protected override void OnStop()
        {
            server.cts.Cancel();
        }
    }
}
