
using ConsoleGames;
using PowerArgs.Cli;
using System;

namespace Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            new PlaygroundGame().Start().Wait();
            return;

            var app = new ConsoleApp();
            app.LayoutRoot.Add(new LevelEditor(75, 20)).CenterVertically().CenterHorizontally();
            app.Start().Wait();
        }
    }
}
