using ArgsTests;
using PowerArgs.Cli;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Threading;

namespace HelloWorld
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Random r = new Random();
            ConsoleApp appy = new ConsoleApp();

            appy.LayoutRoot.Add(new MatrixPanel()).Fill();

            appy.QueueAction(() =>
            {
                Dialog.ShowMessage("Welcome to the Matrix".ToGreen(),
                    (b)=> { }, 
                    true,
                    10, 
                    new DialogButton() { DisplayText = "Red Pill".ToRed() }, new DialogButton() { DisplayText = "Blue Pill".ToDarkCyan() });
            });

            appy.Start().Wait();
            return;

            var viewModel = new BasicXmlAppViewModel();
            var app = ConsoleApp.FromMvVm(ArgsTests.Resources.BasicXmlApp, viewModel);
         
            var task = app.Start();
            task.Wait();
            Console.WriteLine(viewModel.Customer.Name);
            // Samples.SearchSample.Run();
            // Samples.ProgressBarSample.Run();
            // Samples.CalculatorProgramSample._Main(args); // a simple 4 function calculator
            //Samples.HelloWorldParse._Main(args);       //  The simplest way to use the parser.  All this sample does is parse the arguments and send them back to your program.
            // Samples.HelloWorldInvoke._Main(args);      //  A simple way to have the parser parse your arguments and then call a new Main method that you build.
            // Samples.Git._Main(args);                   //  Sample that shows how to implement a program that accepts multiple commands and where each command takes its own set of arguments.
            // Samples.REPLInvoke._Main(args);            //  Sample that shows how to implement a REPL (Read Evaluate Print Loop)
            // Samples.HelloWorldConditionalIInvoke._Main(args);
        }
    }
}
