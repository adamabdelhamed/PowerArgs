using System;
using PowerArgs;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using PowerArgs.Cli;
using System.Threading;
using System.Threading.Tasks;
using HelloWorld.Samples;
using ArgsTests;
using System.Windows.Forms;
using ArgsTests.CLI.ArgsTests.CLI;
using System.Drawing;

namespace HelloWorld
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var console = new WinFormsTestConsole(80, 20) { Width = 1000, Height = 1000 };
            ConsoleProvider.Current = console;
            var viewModel = new BasicXmlAppViewModel();
            var app = ConsoleApp.FromMvVm(ArgsTests.Resources.BasicXmlApp, viewModel);
            WinFormsTestConsole.Run(app, console, () =>
            {
                console.Input.Enqueue("Adam Abdelhamed");
                console.Input.Enqueue(ConsoleKey.Tab);
                console.Input.EnqueueDelay(TimeSpan.FromSeconds(2));
                console.Input.Enqueue(ConsoleKey.Enter);
                console.Input.SimulateUserNow();
            });

            // Samples.SearchSample.Run();
            // Samples.ProgressBarSample.Run();
            // Samples.CalculatorProgramSample._Main(args); // a simple 4 function calculator
            // Samples.HelloWorldParse._Main(args);       //  The simplest way to use the parser.  All this sample does is parse the arguments and send them back to your program.
            // Samples.HelloWorldInvoke._Main(args);      //  A simple way to have the parser parse your arguments and then call a new Main method that you build.
            // Samples.Git._Main(args);                   //  Sample that shows how to implement a program that accepts multiple commands and where each command takes its own set of arguments.
            // Samples.REPLInvoke._Main(args);            //  Sample that shows how to implement a REPL (Read Evaluate Print Loop)
            // Samples.HelloWorldConditionalIInvoke._Main(args);
        }
    }
}
