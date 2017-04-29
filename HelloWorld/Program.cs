using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.IO;

namespace HelloWorld
{
    class Program
    {
        private static void Play()
        {
            var app = new ConsoleApp();
            var player = app.LayoutRoot.Add(new ConsoleBitmapPlayer()).Fill();
            var promise = app.Start();
            player.Load(File.OpenRead(@"C:\temp\firstpowerargsvideo.txt"));
            promise.Wait();
        }

        private static void Record()
        {
            var app = new Samples.AzureTableBrowserApp();
            app.Recorder = new ConsoleBitmapStreamWriter(File.OpenWrite(@"C:\temp\firstpowerargsvideo.txt"));
            var appPromise = app.Start();
            appPromise.Wait();
        }
        [STAThread]
        static void Main(string[] args)
        {

            Play();
            return;

            // Samples.SearchSample.Run();
            // Samples.ProgressBarSample.Run();
            // Samples.CalculatorProgramSample._Main(args); // a simple 4 function calculator
            Samples.HelloWorldParse._Main(args);            //  The simplest way to use the parser.  All this sample does is parse the arguments and send them back to your program.
            // Samples.HelloWorldInvoke._Main(args);        //  A simple way to have the parser parse your arguments and then call a new Main method that you build.
            // Samples.Git._Main(args);                     //  Sample that shows how to implement a program that accepts multiple commands and where each command takes its own set of arguments.
            // Samples.REPLInvoke._Main(args);              //  Sample that shows how to implement a REPL (Read Evaluate Print Loop)
            // Samples.HelloWorldConditionalIInvoke._Main(args);
        }
    }
}
