using BenchmarkDotNet.Attributes;
using PowerArgs;

[MemoryDiagnoser, ShortRunJob]
public class ShapeBenchmarks
{
    [Params("Action only", "Inherited action", "Inherited Main", "Scalar control")]
    public string Scenario { get; set; } = null!;
    private static readonly string[] ActionInput = ["Run", "42"];
    private static readonly string[] MainInput = ["-Count", "42"];
    private static readonly string[] ScalarInput = ["-Count", "42", "-Ratio", "0.75"];

    [Benchmark(Baseline = true)]
    public object Reflection() => Scenario == "Action only" ? Args.InvokeAction<ClassicActionOnly>(ActionInput) :
        Scenario == "Inherited action" ? Args.InvokeAction<ClassicInheritedAction>(ActionInput) :
        Scenario == "Inherited Main" ? Args.InvokeMain<ClassicInheritedMain>(MainInput) : Args.Parse<ExpansionBenchmarks.ClassicScalar>(ScalarInput);

    [Benchmark]
    public object Generated() => Scenario == "Action only" ? Args.InvokeAction<GeneratedActionOnly>(ActionInput) :
        Scenario == "Inherited action" ? Args.InvokeAction<GeneratedInheritedAction>(ActionInput) :
        Scenario == "Inherited Main" ? Args.InvokeMain<GeneratedInheritedMain>(MainInput) : Args.Parse<GeneratedScalar>(ScalarInput);
}

public class ClassicActionOnly
{
    public int Result;
    [ArgActionMethod] public void Run(int count) => Result = count;
}
public partial class GeneratedActionOnly
{
    public int Result;
    [ArgActionMethod] public void Run(int count) => Result = count;
}
public class ClassicInheritedAction : ClassicActionOnly { }
public partial class GeneratedInheritedAction : ClassicActionOnly { }
public class MainBase
{
    public int Count { get; set; }
    public int Result;
    public void Main() => Result = Count;
}
public class ClassicInheritedMain : MainBase { }
public partial class GeneratedInheritedMain : MainBase { }
