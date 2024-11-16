using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ServerControl host = new ServerControl("10.0.0.34", 13000);
            await host.StartServer();
        }
    }
}
