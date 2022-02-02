using PowerArgs;
using PowerArgs.Cli;
namespace Benchmarks;

public class EmptyAppBenchmark : UIBenchmark
{
    protected override float RunActual(ConsoleApp app)
    {
        app.Invoke(async () =>
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < TimeSpan.FromSeconds(3))
            {
                await app.Paint();
            }
            app.Stop();
        });
        app.Run();
        return app.TotalPaints;
    }
}

