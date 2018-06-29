using PowerArgs;
using PowerArgs.Cli;
using System;
using System.IO;

namespace PowerArgsVideoPlayer
{
    class Program
    {
        [ArgExistingFile, ArgPosition(0)]
        public string InputFile { get; set; }
        static void Main(string[] args) => Args.InvokeMain<Program>(args);

        public void Main()
        {
            if(InputFile == null)
            {
                "No input file specified".ToRed().WriteLine();
                return;
            }

            var app = new ConsoleApp();
            var player = app.LayoutRoot.Add(new ConsoleBitmapPlayer()).Fill();
            app.QueueAction(() => player.Load(File.OpenRead(InputFile)));
            app.Start().Wait();
        }
    }
}
