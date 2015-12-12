using System;
using PowerArgs;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using PowerArgs.Cli;
using System.Threading;
using System.Threading.Tasks;
using ArgsTests.Data;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("My App\n\n*****");
            var app = new ConsoleApp(0, ConsoleProvider.Current.CursorTop, ConsoleProvider.Current.BufferWidth, 20);
            var vm = new GridViewModel(new TestLoadMoreDataSource(app.MessagePump,95, TimeSpan.FromSeconds(3)));
            vm.VisibleColumns.Add(new ColumnViewModel("Id".ToConsoleString(ConsoleColor.Yellow)));
            vm.VisibleColumns.Add(new ColumnViewModel("Value".ToConsoleString(ConsoleColor.Yellow)));
            var grid = new Grid(vm) { Width = ConsoleProvider.Current.BufferWidth, Height = 20};

            var label = new Label();
            label.Foreground = new ConsoleCharacter(' ', ConsoleColor.Green);
            label.Width = 15;
            label.Height = 1;
            label.X = app.LayoutRoot.Width - label.Width;
            label.Y = 0;

            app.LayoutRoot.Controls.Add(grid);
            app.LayoutRoot.Controls.Add(label);

            label.Bind(vm, nameof(vm.SelectedItem));

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
