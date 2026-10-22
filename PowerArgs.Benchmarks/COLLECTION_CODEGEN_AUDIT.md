# Direct collection codegen audit — October 22, 2026

Baseline: `30c723f`. Scalar arrays and lists previously selected the generated shared parser, while collection-valued actions fell back to reflection. This expansion moves eligible collections onto the direct parser and generated invocation.

## Capability and implementation

- Rank-one arrays and exact `List<T>` of supported scalar elements, including nullable scalars and enums, now qualify in root properties, positioned arguments, action parameters, action-object properties, and global action options.
- The generator emits typed collection converter delegates. Arrays use `new T[]` and typed assignment; lists use pre-sized `List<T>`. Neither requires reflected collection construction or `Array.SetValue`. Multi-token values use a span over the original input instead of allocating an intermediate parser-state list.
- Element conversion retains the classic reviver service, including dynamically registered scalar revivers and the alias-based element name. This is not a zero-reflection or zero-boxing claim.
- Classic single-token comma splitting, multi-token values, negative numbers, slash boundaries, empty collections, defaults, simple required checks, aliases, duplicate rejection, element conversion errors, and parse-state reset are covered. Explicit long-form collection aliases work with and without `=`. Unaliased long-form collection options remain invalid, as in classic parsing; the earlier direct scalar shorthand remains supported.
- Collection validators/hooks, nested collections, and custom element types deliberately retain the shared parser (or reflection for unsupported action combinations). The pipeline remains selected at compile time.

## Verification and measurements

Run tests with `dotnet test PowerArgsTestCore/PowerArgsTestCore.csproj -c Release`. The complete suite passed 485 tests, with no failures or skips. New paired classic/generated tests cover collection values and failures, action invocation, global options, defaults, nullable enums, custom element revivers, and conservative fallback selection. No timing assertions were added to the tests.

Run benchmarks with `dotnet run -c Release --project PowerArgs.Benchmarks -- --collections-only`. Identical benchmark source was run before the implementation change and after it. BenchmarkDotNet ShortRun uses one launch, three warmups, and three measured iterations. All eight cases completed successfully. No builds or tests ran concurrently with the benchmarks.

The collection inputs contain eight integers, supplied as either one comma-separated string or eight tokens. The action receives a `List<int>` and stores its count; the scalar control parses two ordinary options. These are warm, steady-state measurements on Windows/.NET 10, not cold-start, large-collection, cross-platform, or Native AOT measurements. Full reports, including short-run confidence intervals, follow below.

| Scenario | Previous generated mean | New generated mean | Current classic mean | Speedup vs classic | New / classic allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Array, commas | 2,961.6 ns | 605.8 ns | 2,569.0 ns | 4.2x | 608 / 3,224 |
| Array, multiple tokens | 3,064.1 ns | 576.8 ns | 2,751.3 ns | 4.8x | 328 / 3,256 |
| List-valued action | 4,629.1 ns | 695.9 ns | 4,617.3 ns | 6.6x | 680 / 5,608 |
| Scalar control | 274.5 ns | 292.2 ns | 2,654.0 ns | 9.1x | 80 / 3,065 |

The new collection paths are 4.9–6.7x faster than the previous generated models and allocate 81–90% less than current classic parsing. The scalar control has unchanged allocation; its roughly 18 ns increase has overlapping short-run confidence intervals, so no scalar speed improvement or regression is established by this run.

## Deferred work

Follow-up: action-only scaffolds and inherited entry points are now covered in [the entry-point audit](SHAPE_CODEGEN_AUDIT.md); the list below records the deferrals at the end of the collection expansion.

- **Rich collection lifecycles and custom/nested element types:** validators and hooks can inspect or alter shared parser state. They need explicit lifecycle integration and parity tests before broadening eligibility.
- **Element conversion overhead:** the classic reviver service still boxes some elements and may use component-model reflection. Typed scalar converters could reduce this, but must preserve runtime reviver replacement, nullable semantics, culture, and errors.
- **Other scaffold shapes and generator diagnostics:** propertyless/generic scaffolds, inherited action methods, and actionable fallback diagnostics remain separate generator-shape work. This change stays focused on the common collection gap.
- **Broader performance study:** larger collections, cold starts, Linux/macOS, and AOT remain unmeasured. The previous audit records the other outstanding capability limits.

## Before: 30c723f

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i9-14900HX 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
| Method     | Scenario       | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |--------------- |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Reflection** | **Array comma**    | **2,521.0 ns** | **1,226.37 ns** | **67.22 ns** |  **1.00** |    **0.03** | **0.1678** |    **3224 B** |        **1.00** |
| Generated  | Array comma    | 2,961.6 ns | 1,188.94 ns | 65.17 ns |  1.18 |    0.03 | 0.2289 |    4376 B |        1.36 |
|            |                |            |             |          |       |         |        |           |             |
| **Reflection** | **Array tokens**   | **2,787.1 ns** |   **987.87 ns** | **54.15 ns** |  **1.00** |    **0.02** | **0.1717** |    **3256 B** |        **1.00** |
| Generated  | Array tokens   | 3,064.1 ns |   633.01 ns | 34.70 ns |  1.10 |    0.02 | 0.2327 |    4408 B |        1.35 |
|            |                |            |             |          |       |         |        |           |             |
| **Reflection** | **List action**    | **4,623.7 ns** | **1,387.31 ns** | **76.04 ns** |  **1.00** |    **0.02** | **0.2975** |    **5608 B** |        **1.00** |
| Generated  | List action    | 4,629.1 ns | 1,048.25 ns | 57.46 ns |  1.00 |    0.02 | 0.2975 |    5608 B |        1.00 |
|            |                |            |             |          |       |         |        |           |             |
| **Reflection** | **Scalar control** | **2,625.6 ns** |   **205.65 ns** | **11.27 ns** |  **1.00** |    **0.01** | **0.1602** |    **3065 B** |        **1.00** |
| Generated  | Scalar control |   274.5 ns |    64.88 ns |  3.56 ns |  0.10 |    0.00 | 0.0038 |      80 B |        0.03 |

## After: direct collection implementation

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i9-14900HX 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
| Method     | Scenario       | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |--------------- |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Reflection** | **Array comma**    | **2,569.0 ns** |   **641.25 ns** | **35.15 ns** |  **1.00** |    **0.02** | **0.1678** |    **3224 B** |        **1.00** |
| Generated  | Array comma    |   605.8 ns |   190.96 ns | 10.47 ns |  0.24 |    0.00 | 0.0315 |     608 B |        0.19 |
|            |                |            |             |          |       |         |        |           |             |
| **Reflection** | **Array tokens**   | **2,751.3 ns** |   **205.83 ns** | **11.28 ns** |  **1.00** |    **0.01** | **0.1717** |    **3256 B** |        **1.00** |
| Generated  | Array tokens   |   576.8 ns |   152.58 ns |  8.36 ns |  0.21 |    0.00 | 0.0172 |     328 B |        0.10 |
|            |                |            |             |          |       |         |        |           |             |
| **Reflection** | **List action**    | **4,617.3 ns** | **1,185.80 ns** | **65.00 ns** |  **1.00** |    **0.02** | **0.2975** |    **5608 B** |        **1.00** |
| Generated  | List action    |   695.9 ns |    68.77 ns |  3.77 ns |  0.15 |    0.00 | 0.0353 |     680 B |        0.12 |
|            |                |            |             |          |       |         |        |           |             |
| **Reflection** | **Scalar control** | **2,654.0 ns** | **1,120.95 ns** | **61.44 ns** |  **1.00** |    **0.03** | **0.1602** |    **3065 B** |        **1.00** |
| Generated  | Scalar control |   292.2 ns |   126.05 ns |  6.91 ns |  0.11 |    0.00 | 0.0038 |      80 B |        0.03 |
