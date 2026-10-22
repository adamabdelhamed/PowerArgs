using BenchmarkDotNet.Attributes;
using PowerArgs;

[MemoryDiagnoser]
[ShortRunJob]
public class ExpansionBenchmarks
{
    [Params("Scalar control", "Nullable options", "Main with helper")]
    public string Scenario { get; set; } = null!;

    private static readonly string[] Input = ["-Count", "42", "-Ratio", "0.75"];

    [Benchmark(Baseline = true)]
    public object Reflection() => Scenario == "Main with helper" ? Args.InvokeMain<ClassicHelper>(Input) :
        Scenario == "Nullable options" ? Args.Parse<ClassicNullable>(Input) : Args.Parse<ClassicScalar>(Input);

    [Benchmark]
    public object Generated() => Scenario == "Main with helper" ? Args.InvokeMain<GeneratedHelper>(Input) :
        Scenario == "Nullable options" ? Args.Parse<GeneratedNullable>(Input) : Args.Parse<GeneratedScalar>(Input);

    public class ClassicScalar
    {
        public int Count { get; set; }
        public double Ratio { get; set; }
    }

    public class ClassicNullable
    {
        public int? Count { get; set; }
        public double? Ratio { get; set; }
    }

    public class ClassicHelper
    {
        public int Count { get; set; }
        public double Ratio { get; set; }
        public void Main() => Calculate();
        private double Calculate() => Count * Ratio;
    }
}

public partial class GeneratedScalar
{
    public partial int Count { get; set; }
    public partial double Ratio { get; set; }
}

public partial class GeneratedNullable
{
    public partial int? Count { get; set; }
    public partial double? Ratio { get; set; }
}

public partial class GeneratedHelper
{
    public partial int Count { get; set; }
    public partial double Ratio { get; set; }
    public void Main() => Calculate();
    private double Calculate() => Count * Ratio;
}
