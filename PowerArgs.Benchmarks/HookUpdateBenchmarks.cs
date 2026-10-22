using BenchmarkDotNet.Attributes;
using PowerArgs;

// In-memory late updates isolate assignment/lifecycle costs from human input and console I/O.
[MemoryDiagnoser, ShortRunJob]
public class HookUpdateBenchmarks
{
    [Params("Late update", "Direct control")]
    public string Scenario { get; set; } = null!;
    private static readonly string[] Input = ["-Count", "4"];
    [GlobalSetup]
    public void CheckBehavior()
    {
        if (Args.Parse<ClassicLateUpdate>(Input).Count != 7 || Args.Parse<GeneratedLateUpdate>(Input).Count != 7)
            throw new InvalidOperationException("Late-update benchmark must measure correct assignment");
        if (GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedLateUpdate>() != ArgumentPipelineKind.GeneratedSharedParser || GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedUpdateControl>() != ArgumentPipelineKind.GeneratedDirectParser)
            throw new InvalidOperationException("Unexpected benchmark pipeline");
    }
    [Benchmark(Baseline = true)]
    public object Classic() => Scenario == "Late update" ? Args.Parse<ClassicLateUpdate>(Input) : Args.Parse<ClassicUpdateControl>(Input);
    [Benchmark]
    public object Generated() => Scenario == "Late update" ? Args.Parse<GeneratedLateUpdate>(Input) : Args.Parse<GeneratedUpdateControl>(Input);
}
public class PopulateLate : ArgHook
{
    public override void AfterPopulateProperties(HookContext context) => context.PopulateCurrentArgument("7");
}
public class ClassicLateUpdate { [PopulateLate, ArgRange(1, 10)] public int Count { get; set; } }
public partial class GeneratedLateUpdate { [PopulateLate, ArgRange(1, 10)] public partial int Count { get; set; } }
public class ClassicUpdateControl { public int Count { get; set; } }
public partial class GeneratedUpdateControl { public partial int Count { get; set; } }
