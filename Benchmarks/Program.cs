using BenchmarkDotNet.Running;
using Benchmarks;
new UIBenchmarkRunner().Run();
return;
BenchmarkRunner.Run<FillRectBenchmark>();
BenchmarkRunner.Run<PaintBenchmark>();