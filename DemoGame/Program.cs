using PowerArgs.Games;
using PowerArgs.Cli;
using PowerArgs;
using System;

namespace DemoGame
{
    class Program
    {
        public static void Main(string[] args) => Args.InvokeMain<Prog>(args);
    }

    class Prog
    { 
        public void Main()
        {
            var winSound = new WindowsSoundProvider.SoundProvider();
            Sound.Provider = winSound;
            winSound.StartPromise.Wait();
            new DemoMultiPlayerGameApp().Start().Wait();
            Sound.Dispose();
        }
    }
}
