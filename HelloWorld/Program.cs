using PowerArgs.Cli;
using System;
using PowerArgs;
using System.Collections.Generic;
using PowerArgs.Cli.Physics;
using System.Threading.Tasks;
using System.Linq;
namespace HelloWorld
{
    class Program
    {
        public static class RadialAiming
        {
            public static Direction UpdateAimers(ConsoleControl c, PixelControl leftAimer, PixelControl rightAimer, float angle)
            {
                var slice = Geometry.GetDirection(angle);

                leftAimer.Value = new ConsoleCharacter(CharsTop[slice], ConsoleColor.Magenta);
                leftAimer.X = c.X + XLeftAimer[slice];
                leftAimer.Y = c.Y + YLeftAimer[slice];

                rightAimer.Value = new ConsoleCharacter(CharsTop[slice], ConsoleColor.Magenta);
                rightAimer.X = c.X + XRightAimer[slice];
                rightAimer.Y = c.Y + YRightAimer[slice];
                return slice;
            }

            private static Dictionary<Direction, char> CharsTop = new Dictionary<Direction, char>()
            {
                { Direction.Right, '_' },
                { Direction.RightDown, '\\' },
                { Direction.DownRight, '\\' },
                { Direction.Down, '|' },
                { Direction.DownLeft,'/' },
                { Direction.LeftDown, '/'},
                { Direction.Left,'_'},
                { Direction.LeftUp, '\\' },
                { Direction.UpLeft, '\\' },
                { Direction.Up, '|' },
                { Direction.UpRight, '/' },
                { Direction.RightUp, '/' },
            };

            private static Dictionary<Direction, int> XLeftAimer = new Dictionary<Direction, int>()
            {
                { Direction.Right, 2 },
                { Direction.RightDown, 2 },
                { Direction.DownRight, 0 },
                { Direction.Down, -1 },
                { Direction.DownLeft, -2 },
                { Direction.LeftDown, -2 },
                { Direction.Left, -2 },
                { Direction.LeftUp, -2 },
                { Direction.UpLeft, -2 },
                { Direction.Up, -1 },
                { Direction.UpRight, 0 },
                { Direction.RightUp, 1 },
            };

            private static Dictionary<Direction, int> XRightAimer = new Dictionary<Direction, int>()
            {
                { Direction.Right, 2 },
                { Direction.RightDown, 1 },
                { Direction.DownRight, 2 },
                { Direction.Down, 1 },
                { Direction.DownLeft, 0 },
                { Direction.LeftDown, -1 },
                { Direction.Left, -2 },
                { Direction.LeftUp, -1 },
                { Direction.UpLeft, 0 },
                { Direction.Up, 1 },
                { Direction.UpRight, 2 },
                { Direction.RightUp, 2 },
            };

            private static Dictionary<Direction, int> YLeftAimer = new Dictionary<Direction, int>()
            {
                { Direction.Right, -1 },
                { Direction.RightDown, 0 },
                { Direction.DownRight, 1 },
                { Direction.Down, 1 },
                { Direction.DownLeft, 1 },
                { Direction.LeftDown, 0 },
                { Direction.Left, -1 },
                { Direction.LeftUp, 0 },
                { Direction.UpLeft, -1 },
                { Direction.Up, -1 },
                { Direction.UpRight, -1 },
                { Direction.RightUp, -1 },
            };

            private static Dictionary<Direction, int> YRightAimer = new Dictionary<Direction, int>()
            {
                { Direction.Right, 0 },
                { Direction.RightDown, 1 },
                { Direction.DownRight, 1 },
                { Direction.Down, 1 },
                { Direction.DownLeft, 1 },
                { Direction.LeftDown, 1 },
                { Direction.Left, 0 },
                { Direction.LeftUp, -1 },
                { Direction.UpLeft, -1 },
                { Direction.Up, -1 },
                { Direction.UpRight, -1 },
                { Direction.RightUp, 0 },
            };
        }

        [STAThread]
        static void Main(string[] args)
        {
            ConsoleApp.Show(async (app) =>
            {
                var c = app.LayoutRoot.Add(new Label() { X = 5, Y = 5, Text = "V".ToMagenta()});
                var label = app.LayoutRoot.Add(new Label() { X = 15, Y = 5 });
                var leftAimer = app.LayoutRoot.Add(new PixelControl());
                var rightAimer = app.LayoutRoot.Add(new PixelControl());

                var setter = new Action<int>((angle) => label.Text = RadialAiming.UpdateAimers(c, leftAimer, rightAimer, angle).ToString().ToGreen());

                while (true)
                {
                    await Animator.AnimateAsync(new RoundedAnimatorOptions() { From = 15, To = 90, Duration = 250, Setter = setter, EasingFunction = Animator.Linear });
                    await Task.Delay(500);
                    await Animator.AnimateAsync(new RoundedAnimatorOptions() { From = 90, To = 165, Duration = 250, Setter = setter, EasingFunction = Animator.Linear });
                    await Task.Delay(50);
                    await Animator.AnimateAsync(new RoundedAnimatorOptions() { From = 165, To = 90, Duration = 250, Setter = setter, EasingFunction = Animator.Linear });
                    await Task.Delay(500);
                    await Animator.AnimateAsync(new RoundedAnimatorOptions() { From = 90, To = 15, Duration = 250, Setter = setter, EasingFunction = Animator.Linear });
                    await Task.Delay(50);
                }
            });

            // Samples.SearchSample.Run();
            // Samples.ProgressBarSample.Run();
            // Samples.CalculatorProgramSample._Main(args); // a simple 4 function calculator
            //Samples.HelloWorldParse._Main(args);            //  The simplest way to use the parser.  All this sample does is parse the arguments and send them back to your program.
            // Samples.HelloWorldInvoke._Main(args);        //  A simple way to have the parser parse your arguments and then call a new Main method that you build.
            // Samples.Git._Main(args);                     //  Sample that shows how to implement a program that accepts multiple commands and where each command takes its own set of arguments.
             Samples.REPLInvoke._Main(args);              //  Sample that shows how to implement a REPL (Read Evaluate Print Loop)
            // Samples.HelloWorldConditionalIInvoke._Main(args);
        }


    }
}                                  
 