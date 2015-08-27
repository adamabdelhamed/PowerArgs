using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Threading;

namespace HelloWorld.Samples
{
    public class ProgressBarSample
    {
        public static void Run()
        {
            DeterminateSample();
            IndeterminateSample();
            IndeterminateSample2();
        }

        private static void DeterminateSample()
        {
            Console.WriteLine("This progress bar shows determinate progress and stays visible after the operation is complete");
            
            var bar = new CliProgressBar("My operation is {%} complete");

            var start = DateTime.Now;
            bar.RenderAndPollDeterminate(() =>
            {
                var delta = DateTime.Now - start;
                bar.Progress = Math.Min(delta.TotalSeconds / 4, 1);
                if (delta > TimeSpan.FromSeconds(3))
                {
                    bar.Message = "The operation was cancelled".ToConsoleString();
                    bar.FillColor = ConsoleColor.Red;
                    throw new OperationCanceledException();
                }
            }
            , TimeSpan.FromMilliseconds(100));
        }

        private static void IndeterminateSample()
        {
            Console.WriteLine("\nThis progress bar shows indeterminate progress as long as it takes to run the given action");

            var bar = new CliProgressBar("Please wait");

            bar.RenderUntilIndeterminate(() =>
            {
                bar.Message = "Phase 1".ToConsoleString();
                Thread.Sleep(1000);
                bar.Message = "Phase 2".ToConsoleString();
                Thread.Sleep(1000);
                bar.Message = "Phase 3".ToConsoleString();
                Thread.Sleep(1000);
            });
        }

        private static void IndeterminateSample2()
        {
            Console.WriteLine("\nThis progress bar shows indeterminate progress and removes itself when complete, polling periodically for an update");

            var bar = new CliProgressBar("Please wait");

            int secs = 5;
            bar.RenderAndPollIndeterminate(() =>
            {
                return secs-- > 0;
            }, TimeSpan.FromSeconds(1));
            bar.Wipe();
        }
    }
}
