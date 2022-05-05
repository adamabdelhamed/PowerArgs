using PowerArgs;
using System.Reflection;

namespace Benchmarks;
public class UIBenchmarkRunner
{
    public void Run()
    {
        var toRun = Discover();
        var results = new UIBenchmarkComparison[toRun.Length];
        for(var i = 0; i < toRun.Length; i++)
        {
            results[i] = toRun[i].Run();
            GC.Collect(2, GCCollectionMode.Forced);
        }

        var headers = new List<ConsoleString>()
        {
            "Test".ToYellow(),
            "Work Done".ToYellow(),
            "Speedup".ToYellow(),
        };

        var output = new ConsoleTableBuilder().FormatAsTable(headers, results.Select(r => new List<ConsoleString>()
        {
            r.Test.ToString().ToWhite(),
            r.Temp.WorkDone.ToString("N0").ToWhite(), 
            r.PaintSpeedupString,
        }).ToList());

        output.WriteLine();
    }

    private UIBenchmark[] Discover() => Assembly
        .GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(UIBenchmark)))
        .Where (t => t == typeof(WorstCaseBenchmark))
        .Select(t => Activator.CreateInstance(t) as UIBenchmark)
        .ToArray();
}

