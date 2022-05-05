using BenchmarkDotNet.Running;
using Benchmarks;
using PowerArgs;

//Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
//BenchmarkRunner.Run<ArrayBenchmark>();
new UIBenchmarkRunner().Run();
return;
BenchmarkRunner.Run<FillRectBenchmark>();
BenchmarkRunner.Run<PaintBenchmark>();