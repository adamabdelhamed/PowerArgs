using BenchmarkDotNet.Attributes;
using PowerArgs;

[MemoryDiagnoser, ShortRunJob]
public class LifecycleBenchmarks
{
    [Params("Definition hook", "Collection validator", "Direct control")]
    public string Scenario { get; set; } = null!;
    private static readonly string[] Input = ["Run", "1", "2", "3"];
    private static readonly string[] Scalar = ["Run", "42"];
    [Benchmark(Baseline = true)]
    public object Reflection() => Scenario == "Definition hook" ? Args.InvokeAction<ClassicHookAction>(Scalar) :
        Scenario == "Collection validator" ? Args.InvokeAction<ClassicValidatedAction>(Input) : Args.InvokeAction<ClassicActionOnly>(Scalar);
    [Benchmark]
    public object Generated() => Scenario == "Definition hook" ? Args.InvokeAction<GeneratedHookAction>(Scalar) :
        Scenario == "Collection validator" ? Args.InvokeAction<GeneratedValidatedAction>(Input) : Args.InvokeAction<GeneratedActionOnly>(Scalar);
}
public class LifecycleHook : ArgHook { public override void BeforeParse(HookContext context) { _ = context.CmdLineArgs.Length; } }
[LifecycleHook] public class ClassicHookAction { public int Result; [ArgActionMethod] public void Run(int count) => Result = count; }
[LifecycleHook] public partial class GeneratedHookAction { public int Result; [ArgActionMethod] public void Run(int count) => Result = count; }
public class FirstPositive : ArgValidator { public override void Validate(string name, ref string arg) { if (int.Parse(arg) < 0) throw new ValidationArgException(name); } }
public class ValidatedPayload { [ArgPosition(1), FirstPositive] public int[] Values { get; set; } = null!; }
public class ClassicValidatedAction { public int Result; [ArgActionMethod] public void Run(ValidatedPayload args) => Result = args.Values.Length; }
public partial class GeneratedValidatedAction { public int Result; [ArgActionMethod] public void Run(ValidatedPayload args) => Result = args.Values.Length; }
