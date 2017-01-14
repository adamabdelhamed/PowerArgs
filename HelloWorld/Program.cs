using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace HelloWorld
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var app = new ConsoleApp();
            var scenePanel = app.LayoutRoot.Add(new ScenePanel(32,9) { Width=32,Height=9}).CenterHorizontally().CenterVertically();
            Random r = new Random();
            app.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Spacebar, null, () =>
            {
                scenePanel.Scene.QueueAction(() =>
                {
                    Thing t = new Thing(9f, .01f, .5f, 1f);
                    SceneHelpers.PlaceInEmptyLocation(scenePanel.Scene, t);
                    var speedTracker = new SpeedTracker(t);
                    speedTracker.HitDetectionTypes.Add(typeof(Thing));
                    var gravity = new Gravity(speedTracker);
                    var forwardForce = new Force(speedTracker, r.Next(20, 20), r.Next(210, 330));
                    scenePanel.Scene.Add(t);
                });
            }, app.LifetimeManager);

            var appTask = app.Start();

            app.QueueAction(() =>
            {
                scenePanel.Scene.Start();
            });

            appTask.Wait();
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
