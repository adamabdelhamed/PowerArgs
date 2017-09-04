using PowerArgs;
using PowerArgs.Cli;
using System;
using System.IO;

namespace PowerArgsVideoPlayer
{
    class Program
    {
        [ArgRequired]
        [ArgDefaultValue(@"C:\temp\recordings\TestSeeking.vid")]
        [ArgPosition(0)]
        [ArgExistingFile]
        public string InputFile { get; set; }
        static void Main(string[] args) => Args.InvokeMain<Program>(args);

        public void Main()
        {
            var app = new ConsoleApp();
            var player = app.LayoutRoot.Add(new ConsoleBitmapPlayer()).Fill();
            app.QueueAction(() =>
            {
                player.Load(File.OpenRead(InputFile));
            });

            app.Start().Wait();
        }
    }
}
