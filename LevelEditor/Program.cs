using PowerArgs;
using PowerArgs.Cli;
using System;

namespace LevelEditor
{
    class Program
    {
        [ArgPosition(0)]
        [ArgExistingFile]
        public string InitialFile { get; set; }

        static void Main(string[] args) => Args.InvokeMain<Program>(args);

        public void Main()
        {
            var app = new ConsoleApp();
            app.LayoutRoot.Add(new ConsoleGames.LevelEditor(InitialFile)).Fill();
            app.Start().Wait();
        }
    }
}
