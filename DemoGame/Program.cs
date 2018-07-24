using PowerArgs.Games;
using PowerArgs.Cli;
using PowerArgs;

namespace DemoGame
{
    class Program
    {
        static void Main(string[] args) => Args.InvokeMain<Prog>(args);
    }

    class Prog
    {
        public string RemoteServer { get; set; }
        public void Main()
        {
            new DemoMultiPlayerGameApp(RemoteServer).Start().Wait();
        }
    }
}
