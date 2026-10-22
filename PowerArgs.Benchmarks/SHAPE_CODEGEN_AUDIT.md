# Generated entry-point audit — October 22, 2026

Baseline: `da3af8b`. The generator previously discovered models only through partial properties and looked only at declared methods. Action-only classes without a dummy property stayed on reflection; inherited actions could be omitted from otherwise generated definitions. Inherited Main methods were not emitted as direct invokers.

## Delivered

- A non-generic partial class with public command entry points can opt in without partial properties. This includes parameterless Main, Task-returning Main, action-only classes, inherited instance entry points, nested accessible classes, and classes split across declarations. Ordinary classes and unrelated partial classes retain reflection. Declared public instance properties still must be partial; inherited properties may be ordinary implemented properties.
- Method discovery follows public reflection lookup: derived declarations first, overridden virtual slots represented once, hidden base methods retained, inherited static methods and non-public methods excluded. Generated inherited calls target the declaring type, preserving the selected method even when a derived method hides it.
- Method/type attributes respect inheritance and AttributeUsage multiplicity. Overrides retain inherited action markers and aliases while single-use descriptions can be replaced. Inherited case/usage metadata is emitted, and inherited hooks select the shared lifecycle instead of disappearing.
- Empty action-argument objects are now constructed and passed through ParseAction/Invoke, rather than being passed as null.
- Unsupported inherited actions preserve full reflection fallback. Inaccessible nested classes cannot emit module initializers, and action methods named Main undergo the same signature validation as other actions.

## Tests

`dotnet test PowerArgsTestCore/PowerArgsTestCore.csproj -c Release` passed all 493 tests, with no failures or skips. Eight new test methods cover action-only discovery, multiple declarations, nested classes, synchronous/asynchronous entry points, inherited action order/aliases/help, overrides and hidden methods, case rules, inherited hooks, empty action objects, errors, and conservative fallback. The classic parser is the comparison for inheritance metadata and dispatch behavior. Existing collection/scalar/lifecycle tests remain green. These are executable generator integration tests: the test assembly compiles and runs the emitted code.

## Benchmarks

Run `dotnet run -c Release --project PowerArgs.Benchmarks -- --shapes-only`. Identical benchmark source was run against the baseline and updated implementation. All eight BenchmarkDotNet cases completed, using ShortRun (one launch, three warmups, three measurement iterations). No builds or tests ran concurrently with the benchmark processes. Final additional eligibility guards do not change the generated benchmark methods or runtime paths.

Actions receive one integer and store it in a field; inherited Main reads one integer property and stores it in a field. The scalar control parses two options. These are warm steady-state measurements on Windows/.NET 10, not cold starts or cross-platform/AOT measurements. ShortRun intervals are wide in some cases, so the reported means are workload-specific estimates, not exact universal speedup guarantees.

| Scenario | Previous generated mean | New generated mean | Current classic mean | Speedup vs classic | New / classic allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Action-only class | 2,985.5 ns | 280.6 ns | 2,895.3 ns | 10.3x | 392 / 4,456 |
| Inherited action | 2,973.1 ns | 311.6 ns | 2,996.4 ns | 9.6x | 392 / 4,456 |
| Inherited Main | 1,643.8 ns | 212.1 ns | 1,667.2 ns | 7.9x | 232 / 2,600 |
| Scalar control | 304.5 ns | 282.1 ns | 2,777.0 ns | 9.8x | 80 / 3,065 |

The newly eligible workloads are about 7.8–10.6x faster than their prior generated models, which were actually using reflection. Allocation falls by about 91% versus current classic parsing. The existing scalar control still allocates 80 bytes; its small timing difference is not attributed to a new scalar optimization.

## Remaining work

- Rich action lifecycles and custom collection types still require integration with shared parsing. This change preserves inherited hooks where supported rather than broadening direct parsing across lifecycle contracts.
- Generic/nested-generic scaffolds, legacy property-based action conventions, special accessor shapes, and optional/ref/generic action signatures remain outside this expansion. Generic registration and legacy action mapping require separate designs and compatibility coverage.
- Generator diagnostics explaining fallback reasons remain useful. They need dedicated generator-driver tests and a noise policy for intentional classic models; the existing runtime GetPipelineKind API remains the supported way to inspect selection.
- Typed scalar/collection converters, cold-start measurements, larger workloads, and cross-platform/AOT validation remain separate performance work. No zero-reflection or Native AOT guarantee is made.

## Before: da3af8b

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i9-14900HX 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
| Method     | Scenario         | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |----------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Reflection** | **Action only**      | **3,020.3 ns** |   **653.70 ns** |  **35.83 ns** |  **1.00** |    **0.01** | **0.2365** |    **4456 B** |        **1.00** |
| Generated  | Action only      | 2,985.5 ns |   393.51 ns |  21.57 ns |  0.99 |    0.01 | 0.2365 |    4456 B |        1.00 |
|            |                  |            |             |           |       |         |        |           |             |
| **Reflection** | **Inherited action** | **3,275.0 ns** | **5,574.80 ns** | **305.57 ns** |  **1.01** |    **0.11** | **0.2365** |    **4456 B** |        **1.00** |
| Generated  | Inherited action | 2,973.1 ns |   768.95 ns |  42.15 ns |  0.91 |    0.07 | 0.2365 |    4456 B |        1.00 |
|            |                  |            |             |           |       |         |        |           |             |
| **Reflection** | **Inherited Main**   | **1,666.4 ns** |    **44.61 ns** |   **2.45 ns** |  **1.00** |    **0.00** | **0.1373** |    **2600 B** |        **1.00** |
| Generated  | Inherited Main   | 1,643.8 ns |   256.10 ns |  14.04 ns |  0.99 |    0.01 | 0.1373 |    2600 B |        1.00 |
|            |                  |            |             |           |       |         |        |           |             |
| **Reflection** | **Scalar control**   | **2,614.7 ns** |   **637.20 ns** |  **34.93 ns** |  **1.00** |    **0.02** | **0.1602** |    **3065 B** |        **1.00** |
| Generated  | Scalar control   |   304.5 ns |    78.53 ns |   4.30 ns |  0.12 |    0.00 | 0.0038 |      80 B |        0.03 |

## After: generated entry points

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i9-14900HX 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
| Method     | Scenario         | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |----------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Reflection** | **Action only**      | **2,895.3 ns** |   **874.36 ns** |  **47.93 ns** |  **1.00** |    **0.02** | **0.2365** |    **4456 B** |        **1.00** |
| Generated  | Action only      |   280.6 ns |    50.19 ns |   2.75 ns |  0.10 |    0.00 | 0.0205 |     392 B |        0.09 |
|            |                  |            |             |           |       |         |        |           |             |
| **Reflection** | **Inherited action** | **2,996.4 ns** |   **527.55 ns** |  **28.92 ns** |  **1.00** |    **0.01** | **0.2365** |    **4456 B** |        **1.00** |
| Generated  | Inherited action |   311.6 ns |   393.10 ns |  21.55 ns |  0.10 |    0.01 | 0.0205 |     392 B |        0.09 |
|            |                  |            |             |           |       |         |        |           |             |
| **Reflection** | **Inherited Main**   | **1,667.2 ns** |   **474.99 ns** |  **26.04 ns** |  **1.00** |    **0.02** | **0.1373** |    **2600 B** |        **1.00** |
| Generated  | Inherited Main   |   212.1 ns |   237.62 ns |  13.02 ns |  0.13 |    0.01 | 0.0122 |     232 B |        0.09 |
|            |                  |            |             |           |       |         |        |           |             |
| **Reflection** | **Scalar control**   | **2,777.0 ns** | **1,929.27 ns** | **105.75 ns** |  **1.00** |    **0.05** | **0.1602** |    **3065 B** |        **1.00** |
| Generated  | Scalar control   |   282.1 ns |    66.17 ns |   3.63 ns |  0.10 |    0.00 | 0.0038 |      80 B |        0.03 |
