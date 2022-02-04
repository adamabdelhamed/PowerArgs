using PowerArgs;
using PowerArgs.Cli;
namespace Benchmarks;

public class WorstCaseBenchmark : UIBenchmark
{
    protected override float RunActual(ConsoleApp app)
    {
        app.Invoke(async () =>
        {
            var start = DateTime.UtcNow;
            var panel = app.LayoutRoot.Add(new WorstCasePerfTestPanel()).Fill();
            while(DateTime.UtcNow - start < TimeSpan.FromSeconds(3))
            {
                await app.RequestPaintAsync();
            }
            app.Stop();
        });
        app.Run();
        return app.TotalPaints;
    }
}

