using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Threading.Tasks;

namespace PowerArgs.Samples
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
    public class PerfTest : ConsolePanel
    {
        private PerfTestArgs args;

        public PerfTest(PerfTestArgs args)
        {
            this.args = args;

            this.Ready.SubscribeOnce(Init);
        }



        private TestOptions GetOptionsForArg() => GetType()
                                                    .GetMethod(args.Test.ToString(), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                                    .Invoke(this, new object[0]) as TestOptions;


        private TestOptions LotsOfChanges()
        {
            var testPanel = new WorstCasePerfTestPanel();
            return new TestOptions()
            {
                InitTest = () => testPanel = Add(new WorstCasePerfTestPanel()).Fill(),
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
                    Add(new FallingCharactersPanel(ConsoleColor.Green, ConsoleColor.DarkGreen, ConsoleColor.Black)).Fill();
                }
            };
        }

        private TestOptions BouncingBall()
        {
            return new TestOptions()
            {
                InitTest = async () =>
                {
                    var ball = Add(new Label() { Text = "Bouncing ball".ToMagenta(), Y = 2, X = 1 });
                    await ball.AnimateAsync(new ConsoleControlAnimationOptions()
                    {
                        Loop = Lifetime.Forever,
                        Duration = 1000,
                        Destination = ()=> new RectF((Width - ball.Width) - 1, ball.Y, ball.Width, ball.Height),
                        AutoReverse = true,
                        EasingFunction = Animator.EaseInOut,
                    });
                }
            };
        }

        private async void Init()
        {
            var options = GetOptionsForArg();
            if (args.Mode == ConsoleMode.VirtualTerminal)
            {
                ConsoleProvider.Fancy = true;
            }

            if(args.Mode == ConsoleMode.Console)
            {
                ConsoleProvider.Fancy = false;
            }

            var mechanism = ConsoleProvider.Fancy == false ? "System.Console" : "VirtualTerminal";

            options.InitTest?.Invoke();
            var messagePanel = Add(new ConsolePanel() { Width = 45, Height = 3, Background = ConsoleColor.Red }).CenterBoth();
            var messageLabel = messagePanel.Add(new Label() { Text = "Waiting".ToConsoleString(fg: ConsoleColor.Black, bg: ConsoleColor.Red) }).CenterBoth();

            var now = DateTime.Now;
            var paintsNow = ConsoleApp.Current.TotalPaints;
            while ((DateTime.Now - now).TotalSeconds < 3)
            {
                messageLabel.Text = $"{ConsoleApp.Current.TotalPaints- paintsNow} paints using {mechanism}".ToConsoleString(fg: ConsoleColor.Black, bg: ConsoleColor.Red, true);
                options.OnFrame?.Invoke();
                await Task.Yield();
            }

            var animationPanel = Add(new ConsolePanel() { Background = ConsoleColor.Green, Width = 45, Height = 3 });

            var centerX = ConsoleMath.Round(Width / 2.0 - animationPanel.Width / 2.0);
            var targetY = ConsoleMath.Round((Height / 2.0 - animationPanel.Height / 2) - 5.0);
            animationPanel.X = centerX;
            animationPanel.Y = Height;
            var animationLabel = animationPanel.Add(new Label() { Text = "That's all folks".ToBlack(bg: ConsoleColor.Green) }).CenterBoth();
            await animationPanel.AnimateAsync(new ConsoleControlAnimationOptions()
            {
                Duration = 1000,
                Destination = () => new RectF(centerX, targetY, animationPanel.Width, animationPanel.Height),
            });
            if (animationPanel.IsExpired == false && animationPanel.Parent?.IsExpired == false)
            {
                animationPanel.CenterHorizontally();
            }
        }
    }
    public class TestOptions
    {
        public Action InitTest { get; set; }
        public Action OnFrame { get; set; }
    }
}
 
