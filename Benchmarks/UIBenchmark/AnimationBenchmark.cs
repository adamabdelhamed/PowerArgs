using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace Benchmarks;

public class AnimationBenchmark : UIBenchmark
{
    protected override void RunActual(ConsoleApp app)
    {
        app.Invoke(async () =>
        {
            var square = app.LayoutRoot.Add(new ConsolePanel() { Width = 20, Height = 10, Background = RGB.Green });
            var start = square.ToRectF();
            var dest = new RectF(app.LayoutRoot.Width-square.Width, app.LayoutRoot.Height - square.Height, square.Width, square.Height);
            for (var i = 0; i < 3; i++)
            {
                await square.AnimateAsync(new ConsoleControlAnimationOptions()
                {
                    Destination = () => dest,
                    Duration = 500,
                });

                await square.AnimateAsync(new ConsoleControlAnimationOptions()
                {
                    Destination = () => start,
                    Duration = 500,
                });
            }
            app.Stop();
        });
        app.Run();
    }
}

