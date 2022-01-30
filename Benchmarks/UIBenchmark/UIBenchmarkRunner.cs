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
        }

        var headers = new List<ConsoleString>()
        {
            "Test".ToYellow(),
            "Paints".ToYellow(),
            "Paint Speedup".ToYellow(),
        };

        var output = new ConsoleTableBuilder().FormatAsTable(headers, results.Select(r => new List<ConsoleString>()
        {
            r.Test.ToString().ToWhite(),
            r.Temp.TotalPaints.ToString("N0").ToWhite(), 
            r.PaintSpeedupString,
        }).ToList());

        output.WriteLine();
    }

    private UIBenchmark[] Discover() => Assembly
        .GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(UIBenchmark)))
        .Select(t => Activator.CreateInstance(t) as UIBenchmark)
        .ToArray();
}

