using ConsoleGames;
using PowerArgs.Cli;

namespace DemoGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Sound.Provider = new WindowsSoundProvider.SoundProvider();
            new DemoGameApp().Start().Wait();
            Sound.Dispose();
            return;

            var app = new ConsoleApp();
            app.LayoutRoot.Add(new LevelEditor()).CenterVertically().CenterHorizontally();
            app.Start().Wait();
        }
    }
}
