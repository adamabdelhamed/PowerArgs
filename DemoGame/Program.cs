using ConsoleGames;
using PowerArgs.Cli;

namespace DemoGame
{
    class Program
    {
        static void Main(string[] args)
        {
            var winSound = new WindowsSoundProvider.SoundProvider();
            Sound.Provider = winSound;
            winSound.StartPromise.Wait();
            new DemoGameApp().Start().Wait();
            Sound.Dispose();
            return;
            
            var app = new ConsoleApp();
            app.LayoutRoot.Add(new LevelEditor()).CenterVertically().CenterHorizontally();
            app.Start().Wait();
        }
    }
}
