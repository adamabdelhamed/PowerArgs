using System;
using PowerArgs;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using PowerArgs.Cli;
using System.Threading;
using System.Threading.Tasks;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("My App\n\n*****");
            var promise = new DataItemsPromise();
            var items = new List<Object>()
            {
                new { First = "Adam", Last = "Abdelhamed", Address = "Somewhere in Washington" },
                promise,      
            };

            var vm = new GridViewModel(items);
            var grid = new Grid() { ViewModel = vm, Width = ConsoleProvider.Current.BufferWidth, Height = 20};
            var app = new ConsoleApp(0, ConsoleProvider.Current.CursorTop, ConsoleProvider.Current.BufferWidth, 20);
            app.LayoutRoot.Controls.Add(grid);

            var appTask = app.Start();

            int i = 0;
            app.MessagePump.SetInterval(() =>
            {
                var previousPromise = promise;
                items.RemoveAt(items.Count - 1);
                items.Add(new { First = "Person " + i, Last = "LastName", Address = "Address Here" });
                promise = new DataItemsPromise();
                items.Add(promise);

                previousPromise.TriggerReady();
                i++;
               
            }, TimeSpan.FromSeconds(1));

            appTask.Wait();

            return;
            var logFile = @"C:\temp\powerargslog.txt";
            File.Delete(logFile);
            PowerLogger.LogFile = logFile;
            Samples.CPUAndMemoryChartSample.Run();
            // Samples.SearchSample.Run();
            // Samples.ProgressBarSample.Run();
            // Samples.Piping._Main(args);
            // Samples.CalculatorProgramSample._Main(args); // a simple 4 function calculator
            // Samples.HelloWorldParse._Main(args);       //  The simplest way to use the parser.  All this sample does is parse the arguments and send them back to your program.
            // Samples.HelloWorldInvoke._Main(args);      //  A simple way to have the parser parse your arguments and then call a new Main method that you build.
            // Samples.Git._Main(args);                   //  Sample that shows how to implement a program that accepts multiple commands and where each command takes its own set of arguments.
            // Samples.REPLInvoke._Main(args);            //  Sample that shows how to implement a REPL (Read Evaluate Print Loop)
            // Samples.HelloWorldConditionalIInvoke._Main(args);
        }
    }
}
