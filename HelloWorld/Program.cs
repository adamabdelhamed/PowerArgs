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
           
            var app = new ConsoleApp(0, ConsoleProvider.Current.CursorTop, ConsoleProvider.Current.BufferWidth, 20);
            app.LayoutRoot.Controls.Add(new TextBox() { Width = 40 });
            app.LayoutRoot.Controls.Add(new TextBox() { Y = 1, Width = 40 });
            var appTask = app.Start();
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
