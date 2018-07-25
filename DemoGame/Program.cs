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
        public void Main()
        {
            new DemoMultiPlayerGameApp().Start().Wait();
        }
    }
}
