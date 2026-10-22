using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using PowerArgs;

var config = args.Contains("--profile") ? DefaultConfig.Instance.AddDiagnoser(new EventPipeProfiler(EventPipeProfile.CpuSampling)) : DefaultConfig.Instance;
if (args.Contains("--builtins-only")) BenchmarkRunner.Run<BuiltinBenchmarks>(config);
else if (args.Contains("--lifecycle-only")) BenchmarkRunner.Run<LifecycleBenchmarks>(config);
else if (args.Contains("--shapes-only")) BenchmarkRunner.Run<ShapeBenchmarks>(config);
else if (args.Contains("--collections-only")) BenchmarkRunner.Run<CollectionBenchmarks>(config);
else if (args.Contains("--expansion-only")) BenchmarkRunner.Run<ExpansionBenchmarks>(config);
else if (args.Contains("--invoke-only")) BenchmarkRunner.Run<InvocationBenchmarks>(config);
else if (args.Contains("--parse-only")) BenchmarkRunner.Run<ParseBenchmarks>(config);
else if (args.Contains("--conversion-only")) BenchmarkRunner.Run<ConversionBenchmarks>(config);
else if (args.Contains("--template-only")) BenchmarkRunner.Run<TemplateBenchmarks>(config);
else
{
    BenchmarkRunner.Run<ParseBenchmarks>(config);
    BenchmarkRunner.Run<InvocationBenchmarks>(config);
    BenchmarkRunner.Run<ConversionBenchmarks>(config);
    BenchmarkRunner.Run<TemplateBenchmarks>(config);
}

[MemoryDiagnoser]
[ShortRunJob]
public class TemplateBenchmarks
{
    private const string Template = """
        {{if ShowHeader}}
        {{Title!}}
        !{{if}}
        {{each item in Items}}
        {{item.Name!}}: {{item.Description!}}
        {{if item.Enabled}}[enabled]!{{if}}
        !{{each}}
        """;

    private readonly DocumentRenderer renderer = new();
    private readonly DocumentExpressionParser parser = new();
    private readonly object data = new
    {
        ShowHeader = true,
        Title = "Commands",
        Items = Enumerable.Range(0, 20).Select(i => new { Name = "command-" + i, Description = "Description for command " + i, Enabled = i % 2 == 0 }).ToArray(),
    };
    private readonly List<DocumentToken> tokens = DocumentToken.Tokenize(Template, "benchmark");

    [Benchmark]
    public List<DocumentToken> Tokenize() => DocumentToken.Tokenize(Template, "benchmark");

    [Benchmark]
    public List<IDocumentExpression> Parse() => parser.Parse(tokens);

    [Benchmark]
    public ConsoleString Render() => renderer.Render(Template, data, "benchmark");
}

[MemoryDiagnoser]
[ShortRunJob]
public class ConversionBenchmarks
{
    private const string Raw = "deploy --source \"C:\\Program Files\\PowerArgs\\input.txt\" --count 42 --label \"nightly build\" --enabled";
    private static readonly string[] Values = ["deploy", "--source", "C:\\Program Files\\PowerArgs\\input.txt", "--count", "42", "--label", "nightly build", "--enabled"];

    [Params("Split", "Join")]
    public string Scenario { get; set; } = null!;

    [Benchmark(Baseline = true)]
    public object Legacy() => Scenario == "Split" ? LegacySplit(Raw) : LegacyJoin(Values);

    [Benchmark]
    public object Optimized() => Scenario == "Split" ? Args.Convert(Raw) : Args.Convert(Values);

    private static string[] LegacySplit(string commandLine)
    {
        var ret = new List<string>();
        var currentArg = string.Empty;
        var insideDoubleQuotes = false;
        for (var i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];
            if (insideDoubleQuotes && c == '"') { ret.Add(currentArg); currentArg = string.Empty; insideDoubleQuotes = false; }
            else if (insideDoubleQuotes == false && c == ' ') { if (currentArg.Length > 0) { ret.Add(currentArg); currentArg = string.Empty; } }
            else if (c == '"') insideDoubleQuotes = !insideDoubleQuotes;
            else if (c == '\\' && i < commandLine.Length - 1 && commandLine[i + 1] == '"') { currentArg += '"'; i++; }
            else currentArg += c;
        }
        if (currentArg.Length > 0) ret.Add(currentArg);
        return ret.ToArray();
    }

    private static string LegacyJoin(string[] commandLine)
    {
        var ret = string.Empty;
        for (var i = 0; i < commandLine.Length; i++)
        {
            var element = commandLine[i];
            if (string.IsNullOrWhiteSpace(element)) continue;
            ret += element.Any(char.IsWhiteSpace) ? "\"" + element + "\"" : element;
            if (i < commandLine.Length - 1) ret += " ";
        }
        return ret;
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class ParseBenchmarks
{
    public IEnumerable<ParseScenario> Scenarios()
    {
        yield return new ParseScenario("Required scalars", typeof(ReflectionBasicArgs), typeof(GeneratedBasicArgs), ["-RequiredValue", "hello", "-Count", "42", "-Double", "33.33", "-Bool"]);
        yield return new ParseScenario("Position + shortcuts", typeof(ReflectionPositionShortcutArgs), typeof(GeneratedPositionShortcutArgs), ["input.txt", "-n", "7", "--verbose"]);
        yield return new ParseScenario("Range validation", typeof(ReflectionValidatedArgs), typeof(GeneratedValidatedArgs), ["-Count", "7", "-r", "0.75"]);
        yield return new ParseScenario("Case + ignored", typeof(ReflectionCaseArgs), typeof(GeneratedCaseArgs), ["-v", "exact", "-Enabled"]);
        yield return new ParseScenario("Defaults + rich scalars", typeof(ReflectionRichArgs), typeof(GeneratedRichArgs), ["-Tag", "build-42", "-Mode", "Safe", "-Endpoint", "https://powerargs.test/api"]);
    }

    [ParamsSource(nameof(Scenarios))]
    public ParseScenario Scenario { get; set; } = null!;

    [Benchmark(Baseline = true)]
    public object Reflection() => Args.Parse(Scenario.ReflectionType, Scenario.Input);

    [Benchmark]
    public object Generated() => Args.Parse(Scenario.GeneratedType, Scenario.Input);
}

public sealed class ParseScenario
{
    public string Name { get; }
    public Type ReflectionType { get; }
    public Type GeneratedType { get; }
    public string[] Input { get; }

    public ParseScenario(string name, Type reflectionType, Type generatedType, string[] input)
    {
        Name = name;
        ReflectionType = reflectionType;
        GeneratedType = generatedType;
        Input = input;
    }

    public override string ToString() => Name;
}

public class ReflectionBasicArgs
{
    [ArgRequired]
    public string RequiredValue { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Double { get; set; }
    public bool Bool { get; set; }
}

public partial class GeneratedBasicArgs
{
    [ArgRequired]
    public partial string RequiredValue { get; set; }
    public partial int Count { get; set; }
    public partial double Double { get; set; }
    public partial bool Bool { get; set; }
}

[ArgDescription("Position and shortcut benchmark")]
public class ReflectionPositionShortcutArgs
{
    [ArgRequired, ArgPosition(0), ArgDescription("Input path")]
    public string Input { get; set; } = string.Empty;
    [ArgShortcut("n"), ArgRange(1, 10)]
    public int Count { get; set; }
    [ArgShortcut(ArgShortcutPolicy.ShortcutsOnly), ArgShortcut("--verbose")]
    public bool Verbose { get; set; }
}

[ArgDescription("Position and shortcut benchmark")]
public partial class GeneratedPositionShortcutArgs
{
    [ArgRequired, ArgPosition(0), ArgDescription("Input path")]
    public partial string Input { get; set; }
    [ArgShortcut("n"), ArgRange(1, 10)]
    public partial int Count { get; set; }
    [ArgShortcut(ArgShortcutPolicy.ShortcutsOnly), ArgShortcut("--verbose")]
    public partial bool Verbose { get; set; }
}

public class ReflectionValidatedArgs
{
    [ArgRange(1, 10)]
    public int Count { get; set; }
    [ArgShortcut("r"), ArgRange(0, 1)]
    public double Ratio { get; set; }
}

public partial class GeneratedValidatedArgs
{
    [ArgRange(1, 10)]
    public partial int Count { get; set; }
    [ArgShortcut("r"), ArgRange(0, 1)]
    public partial double Ratio { get; set; }
}

[ArgEnforceCase]
public class ReflectionCaseArgs
{
    [ArgRequired, ArgShortcut("v")]
    public string Value { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    [ArgIgnore]
    public object? Ignored { get; set; }
}

[ArgEnforceCase]
public partial class GeneratedCaseArgs
{
    [ArgRequired, ArgShortcut("v")]
    public partial string Value { get; set; }
    public partial bool Enabled { get; set; }
    [ArgIgnore]
    public partial object? Ignored { get; set; }
}

public enum BenchmarkMode
{
    Fast,
    Safe,
}

[ArgExample("tool -Tag build-42", "Runs a tagged build")]
public class ReflectionRichArgs
{
    [DefaultValue(5), ArgRange(1, 10)]
    public int Count { get; set; }
    [ArgRequired, ArgRegex(@"build-\d+")]
    public string Tag { get; set; } = string.Empty;
    public BenchmarkMode Mode { get; set; }
    public Uri Endpoint { get; set; } = null!;
}

[ArgExample("tool -Tag build-42", "Runs a tagged build")]
public partial class GeneratedRichArgs
{
    [DefaultValue(5), ArgRange(1, 10)]
    public partial int Count { get; set; }
    [ArgRequired, ArgRegex(@"build-\d+")]
    public partial string Tag { get; set; }
    public partial BenchmarkMode Mode { get; set; }
    public partial Uri Endpoint { get; set; }
}

[MemoryDiagnoser]
[ShortRunJob]
public class InvocationBenchmarks
{
    public IEnumerable<InvocationScenario> Scenarios()
    {
        yield return new InvocationScenario("InvokeMain", false, typeof(ReflectionMainArgs), typeof(GeneratedMainArgs), ["-Value", "hello"]);
        yield return new InvocationScenario("InvokeAction", true, typeof(ReflectionActionArgs), typeof(GeneratedActionArgs), ["run", "-Enabled"]);
        yield return new InvocationScenario("Async Main", false, typeof(ReflectionAsyncMainArgs), typeof(GeneratedAsyncMainArgs), []);
        yield return new InvocationScenario("Async Action", true, typeof(ReflectionAsyncActionArgs), typeof(GeneratedAsyncActionArgs), ["Run"]);
        yield return new InvocationScenario("Model Params", true, typeof(ReflectionModelActionArgs), typeof(GeneratedModelActionArgs), ["run", "-Enabled", "-Name", "Ada", "-Count", "3"]);
        yield return new InvocationScenario("Direct Params", true, typeof(ReflectionDirectActionArgs), typeof(GeneratedDirectActionArgs), ["Copy", "-source", "input.txt", "-count", "4"]);
        yield return new InvocationScenario("Validated Defaults", true, typeof(ReflectionValidatedActionArgs), typeof(GeneratedValidatedActionArgs), ["Copy", "-source", "input.txt"]);
    }

    [ParamsSource(nameof(Scenarios))]
    public InvocationScenario Scenario { get; set; } = null!;

    [Benchmark(Baseline = true)]
    public ArgAction Reflection() => Scenario.IsAction ? Args.InvokeAction(Scenario.ReflectionType, Scenario.Input) : Args.InvokeMain(Scenario.ReflectionType, Scenario.Input);

    [Benchmark]
    public ArgAction Generated() => Scenario.IsAction ? Args.InvokeAction(Scenario.GeneratedType, Scenario.Input) : Args.InvokeMain(Scenario.GeneratedType, Scenario.Input);
}

public sealed class InvocationScenario
{
    public string Name { get; }
    public bool IsAction { get; }
    public Type ReflectionType { get; }
    public Type GeneratedType { get; }
    public string[] Input { get; }

    public InvocationScenario(string name, bool isAction, Type reflectionType, Type generatedType, string[] input)
    {
        Name = name;
        IsAction = isAction;
        ReflectionType = reflectionType;
        GeneratedType = generatedType;
        Input = input;
    }

    public override string ToString() => Name;
}

public class ReflectionMainArgs
{
    [ArgRequired]
    public string Value { get; set; } = string.Empty;
    [ArgIgnore]
    public int InvocationCount { get; set; }
    public void Main() => InvocationCount++;
}

public partial class GeneratedMainArgs
{
    [ArgRequired]
    public partial string Value { get; set; }
    [ArgIgnore]
    public partial int InvocationCount { get; set; }
    public void Main() => InvocationCount++;
}

public class ReflectionActionArgs
{
    public bool Enabled { get; set; }
    [ArgIgnore]
    public int InvocationCount { get; set; }
    [ArgActionMethod, ArgShortcut("run")]
    public void Execute() => InvocationCount++;
}

public partial class GeneratedActionArgs
{
    public partial bool Enabled { get; set; }
    [ArgIgnore]
    public partial int InvocationCount { get; set; }
    [ArgActionMethod, ArgShortcut("run")]
    public void Execute() => InvocationCount++;
}

public class ReflectionAsyncMainArgs
{
    [ArgIgnore]
    public int Marker { get; set; }
    public Task Main() => Task.CompletedTask;
}

public partial class GeneratedAsyncMainArgs
{
    [ArgIgnore]
    public partial int Marker { get; set; }
    public Task Main() => Task.CompletedTask;
}

public class ReflectionAsyncActionArgs
{
    [ArgIgnore]
    public int Marker { get; set; }
    [ArgActionMethod]
    public Task Run() => Task.CompletedTask;
}

public partial class GeneratedAsyncActionArgs
{
    [ArgIgnore]
    public partial int Marker { get; set; }
    [ArgActionMethod]
    public Task Run() => Task.CompletedTask;
}

public sealed class ReflectionRunArgs
{
    [ArgRequired]
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ReflectionModelActionArgs
{
    public bool Enabled { get; set; }
    [ArgActionMethod, ArgShortcut("run")]
    public void Execute(ReflectionRunArgs args) { }
}

public sealed class GeneratedRunArgs
{
    [ArgRequired]
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public partial class GeneratedModelActionArgs
{
    public partial bool Enabled { get; set; }
    [ArgActionMethod, ArgShortcut("run")]
    public void Execute(GeneratedRunArgs args) { }
}

public class ReflectionDirectActionArgs
{
    [ArgActionMethod]
    public void Copy([ArgRequired] string source, int count) { }
}

public partial class GeneratedDirectActionArgs
{
    [ArgIgnore]
    public partial int Marker { get; set; }
    [ArgActionMethod]
    public void Copy([ArgRequired] string source, int count) { }
}

public class ReflectionValidatedActionArgs
{
    [ArgActionMethod]
    public void Copy([ArgRequired, ArgRegex(@".+\.txt")] string source, [DefaultValue(2), ArgRange(1, 10)] int count, [ArgDefaultValue(BenchmarkMode.Safe)] BenchmarkMode mode) { }
}

public partial class GeneratedValidatedActionArgs
{
    [ArgIgnore]
    public partial int Marker { get; set; }
    [ArgActionMethod]
    public void Copy([ArgRequired, ArgRegex(@".+\.txt")] string source, [DefaultValue(2), ArgRange(1, 10)] int count, [ArgDefaultValue(BenchmarkMode.Safe)] BenchmarkMode mode) { }
}
