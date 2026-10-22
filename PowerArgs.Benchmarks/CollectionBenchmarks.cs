using BenchmarkDotNet.Attributes;
using PowerArgs;

[MemoryDiagnoser]
[ShortRunJob]
public class CollectionBenchmarks
{
    [Params("Array comma", "Array tokens", "List action", "Scalar control")]
    public string Scenario { get; set; } = null!;

    private static readonly string[] Comma = ["-Values", "1,2,3,4,5,6,7,8"];
    private static readonly string[] Tokens = ["-Values", "1", "2", "3", "4", "5", "6", "7", "8"];
    private static readonly string[] Action = ["Run", "1", "2", "3", "4", "5", "6", "7", "8"];
    private static readonly string[] Scalar = ["-Count", "42", "-Ratio", "0.75"];

    [Benchmark(Baseline = true)]
    public object Reflection() => Scenario == "List action" ? Args.InvokeAction<ClassicCollectionAction>(Action) :
        Scenario == "Scalar control" ? Args.Parse<ExpansionBenchmarks.ClassicScalar>(Scalar) :
        Args.Parse<ClassicCollection>(Scenario == "Array comma" ? Comma : Tokens);

    [Benchmark]
    public object Generated() => Scenario == "List action" ? Args.InvokeAction<GeneratedCollectionAction>(Action) :
        Scenario == "Scalar control" ? Args.Parse<GeneratedScalar>(Scalar) :
        Args.Parse<GeneratedCollection>(Scenario == "Array comma" ? Comma : Tokens);
}

public class ClassicCollection
{
    public int[] Values { get; set; } = null!;
}

public partial class GeneratedCollection
{
    public partial int[] Values { get; set; }
}

public class ClassicCollectionAction
{
    [ArgIgnore] public int Count { get; set; }
    [ArgActionMethod] public void Run(List<int> values) => Count = values.Count;
}

public partial class GeneratedCollectionAction
{
    [ArgIgnore] public partial int Count { get; set; }
    [ArgActionMethod] public void Run(List<int> values) => Count = values.Count;
}
