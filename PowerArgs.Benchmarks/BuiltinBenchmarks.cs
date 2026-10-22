using BenchmarkDotNet.Attributes;
using PowerArgs;

[MemoryDiagnoser, ShortRunJob]
public class BuiltinBenchmarks
{
    [Params("Conditional", "Exclusion", "Validated collection", "Help false")]
    public string Scenario { get; set; } = null!;
    private static readonly string[] ConditionalInput = ["-Enabled", "-Name", "release"];
    private static readonly string[] ExclusionInput = ["-Left", "release"];
    private static readonly string[] CollectionInput = ["-Values", "4", "5", "6"];
    private static readonly string[] HelpInput = ["-Help", "false", "-Count", "4"];
    [Benchmark(Baseline = true)]
    public object Classic() => Scenario == "Conditional" ? Args.Parse<ClassicBuiltinConditional>(ConditionalInput) :
        Scenario == "Exclusion" ? Args.Parse<ClassicBuiltinExclusion>(ExclusionInput) :
        Scenario == "Validated collection" ? Args.Parse<ClassicBuiltinCollection>(CollectionInput) : Args.Parse<ClassicBuiltinHelp>(HelpInput);
    [Benchmark]
    public object Generated() => Scenario == "Conditional" ? Args.Parse<GeneratedBuiltinConditional>(ConditionalInput) :
        Scenario == "Exclusion" ? Args.Parse<GeneratedBuiltinExclusion>(ExclusionInput) :
        Scenario == "Validated collection" ? Args.Parse<GeneratedBuiltinCollection>(CollectionInput) : Args.Parse<GeneratedBuiltinHelp>(HelpInput);
}
public class ClassicBuiltinConditional { public bool Enabled { get; set; } [ArgRequired(If = "Enabled")] public string? Name { get; set; } }
public partial class GeneratedBuiltinConditional { public partial bool Enabled { get; set; } [ArgRequired(If = "Enabled")] public partial string? Name { get; set; } }
public class ClassicBuiltinExclusion { [ArgCantBeCombinedWith("Right")] public string? Left { get; set; } public string? Right { get; set; } }
public partial class GeneratedBuiltinExclusion { [ArgCantBeCombinedWith("Right")] public partial string? Left { get; set; } public partial string? Right { get; set; } }
public class ClassicBuiltinCollection { [ArgRange(1, 10)] public int[]? Values { get; set; } }
public partial class GeneratedBuiltinCollection { [ArgRange(1, 10)] public partial int[]? Values { get; set; } }
public class ClassicBuiltinHelp { [HelpHook(WriteHelp = false)] public bool Help { get; set; } [ArgRange(1, 10)] public int Count { get; set; } }
public partial class GeneratedBuiltinHelp { [HelpHook(WriteHelp = false)] public partial bool Help { get; set; } [ArgRange(1, 10)] public partial int Count { get; set; } }
