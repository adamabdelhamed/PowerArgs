using PowerArgs;
using System;

namespace HelloWorld.Samples
{
    public class ProgressBarSample
    {
        public static void Run()
        {
            DeterminateSample();
            IndeterminateSample();
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
            Console.WriteLine("\nThis progress bar shows indeterminate progress and removes itself when complete");

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
