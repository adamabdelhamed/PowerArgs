using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Threading.Tasks;

namespace Samples
{
    public enum ConsoleMode
    {
        Console,
        VirtualTerminal
    }

    public enum TestCase
    {
        MinimumChanges,
        LotsOfChanges,
        BouncingBall,
        FallingChars
    }

    public class PerfTestArgs
    {
        [ArgDefaultValue(ConsoleMode.Console)]
        public ConsoleMode Mode { get; set; }

        [ArgDefaultValue(TestCase.MinimumChanges)]
        public TestCase Test { get; set; }
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class PerfTest
    {
        private PerfTestArgs args;

        public PerfTest(PerfTestArgs args)
        {
            this.args = args;
        }

        public Promise Start()
        {
            var app = new ConsoleApp();
            app.QueueAction(Init);
            return app.Start();
        }

        private TestOptions GetOptionsForArg() => GetType()
                                                    .GetMethod(args.Test.ToString(), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                                    .Invoke(this, new object[0]) as TestOptions;


        private TestOptions LotsOfChanges()
        {
            var testPanel = new PerfTestPanel();
            return new TestOptions()
            {
                InitTest = () => testPanel = ConsoleApp.Current.LayoutRoot.Add(new PerfTestPanel()).Fill(),
                OnFrame = () => testPanel.Even = !testPanel.Even
            };
        }

        private TestOptions MinimumChanges()
        {
            return new TestOptions();
        }

        private TestOptions FallingChars()
        {
            return new TestOptions()
            {
                InitTest = () =>
                {
                    ConsoleApp.Current.LayoutRoot.Add(new FallingCharactersPanel(ConsoleColor.Green, ConsoleColor.DarkGreen, ConsoleColor.Black)).Fill();
                }
            };
        }

        private TestOptions BouncingBall()
        {
            return new TestOptions()
            {
                InitTest = async () =>
                {
                    var ball = ConsoleApp.Current.LayoutRoot.Add(new Label() { Text = "Bouncing ball".ToMagenta(), Y = 2, X = 1 });
                    await ball.AnimateAsync(new ConsoleControlAnimationOptions()
                    {
                        Loop = Lifetime.Forever,
                        Duration = 1000,
                        Destination = RectangularF.Create((ConsoleApp.Current.LayoutRoot.Width - ball.Width) - 1, ball.Y, ball.Width, ball.Height),
                        AutoReverse = true,
                        EasingFunction = Animator.EaseInOut,
                    });
                }
            };
        }

        private async void Init()
        {
            var options = GetOptionsForArg();
            if (args.Mode == ConsoleMode.VirtualTerminal && ConsoleProvider.TryEnableFancyRendering() == false)
            {
                "Unable to configure Ansi output mode".ToRed().WriteLine();
                System.Environment.Exit(1);
                return;
            }

            var mechanism = ConsoleProvider.Renderer == null ? "System.Console" : "VirtualTerminal";

            options.InitTest?.Invoke();
            var messagePanel = ConsoleApp.Current.LayoutRoot.Add(new ConsolePanel() { Width = 45, Height = 3, Background = ConsoleColor.Red }).CenterBoth();
            var messageLabel = messagePanel.Add(new Label() { Text = "Waiting".ToConsoleString(fg: ConsoleColor.Black, bg: ConsoleColor.Red) }).CenterBoth();

            var now = DateTime.Now;
            while ((DateTime.Now - now).TotalSeconds < 3)
            {
                messageLabel.Text = $"{ConsoleApp.Current.TotalPaints} paints using {mechanism}".ToConsoleString(fg: ConsoleColor.Black, bg: ConsoleColor.Red, true);
                options.OnFrame?.Invoke();
                await Task.Yield();
            }

            var animationPanel = ConsoleApp.Current.LayoutRoot.Add(new ConsolePanel() { Background = ConsoleColor.Green, Width = 45, Height = 3 });

            var centerX = (int)Math.Round(ConsoleApp.Current.LayoutRoot.Width / 2.0 - animationPanel.Width / 2.0);
            var targetY = (int)Math.Round((ConsoleApp.Current.LayoutRoot.Height / 2.0 - animationPanel.Height / 2) - 5.0);
            animationPanel.X = centerX;
            animationPanel.Y = ConsoleApp.Current.LayoutRoot.Height;
            var animationLabel = animationPanel.Add(new Label() { Text = "Press escape to exit".ToBlack(bg: ConsoleColor.Green) }).CenterBoth();
            await animationPanel.AnimateAsync(new ConsoleControlAnimationOptions()
            {
                Duration = 1000,
                Destination = RectangularF.Create(centerX, targetY, animationPanel.Width, animationPanel.Height),
            });
            animationPanel.CenterHorizontally();
        }
    }
    public class TestOptions
    {
        public Action InitTest { get; set; }
        public Action OnFrame { get; set; }
    }
}
 
