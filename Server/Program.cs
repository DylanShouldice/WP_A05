namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServerControl host = new ServerControl();
            host.FindHostIP(); 
            host.Start();
        }
    }
}
