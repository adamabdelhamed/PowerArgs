# Generated pipeline audit — October 22, 2026

The starting point was `d2219e3`. The generated pipeline already had broad feature coverage, but `IsGenerated` concealed a major distinction: simple models used the direct parser, while many common shapes used the shared parser or silently fell back to reflection. The benchmark named "Generated" therefore did not necessarily measure generated direct parsing.

## Delivered

- Nullable scalar options and action parameters, decimal/character values, and the remaining signed/unsigned integer types now qualify for the direct parser. They reuse classic conversion services where necessary, preserving custom revivers, cultures, constants, nullable behavior, and error messages rather than duplicating conversion rules. Nullable booleans remain value-taking options, as in classic PowerArgs.
- Ordinary helper methods no longer disqualify an otherwise eligible scaffold. Generated `Main` and action methods can call helpers without paying reflection discovery/assignment/invocation costs.
- Action argument objects include inherited public properties. Property order follows reflection, so alias allocation is consistent; overrides are emitted once with their validation metadata. Previously base properties could be omitted entirely.
- User scalar revivers now take precedence over built-in fast conversions, including overrides registered after the generated model has already parsed input. The reviver receives the command-line alias rather than the CLR property name when `ShortcutsOnly` is used.
- Nullable type inspection is cached instead of repeatedly constructing generic types. This also improves the classic parser.
- `GeneratedArgumentModelRegistry.GetPipelineKind<T>()` distinguishes `Reflection`, `GeneratedSharedParser`, and `GeneratedDirectParser` without parsing. The README now describes limitations and combinations more precisely.

## Verification

`dotnet test PowerArgsTestCore/PowerArgsTestCore.csproj -c Release` passed all 463 tests, with no failures or skips. The 17 added cases exercise nullable values and omission/reset, constants, empty values, errors, numeric bounds, culture, helper invocation, inherited/overridden action properties and validation, nullable action parameters, tier reporting, and dynamic custom reviver replacement/restoration. Existing coverage continues to exercise hooks, collections, usage, action parsing, defaults, and classic compatibility.

Benchmarks use `dotnet run -c Release --project PowerArgs.Benchmarks -- --expansion-only`. They are BenchmarkDotNet ShortRun jobs with allocation diagnostics: one process launch, three warmups and three measurement iterations per case. They measure steady-state parsing/invocation, not cold startup or assembly load. Input contains two options (`Count=42`, `Ratio=0.75`). The baseline uses the same benchmark source against the pre-change implementation; the final comparison uses classic and generated paths in the updated codebase. The scalar control was already on the direct path before this work.

Environment: Windows 11, Intel Core i9-14900HX, .NET SDK 10.0.301 / runtime 10.0.9, x64 Release. These short local measurements establish the scale of the gain; small timing differences should not be interpreted as statistically proven improvements. No Linux, macOS, Native AOT, or cold-start benchmark was run.

The final nullable case measures 502.8 ns and 88 B per call, versus 7,200.2 ns / 5,632 B on the previous generated path (about 14.3x faster), and 2,810.5 ns / 2,504 B on current classic parsing (about 5.6x faster and 96.5% less allocation). The helper-bearing Main case measures 348.3 ns / 264 B, versus current classic 2,779.7 ns / 3,321 B (about 8x faster and 92% less allocation). Nullable caching also improved classic parsing from 6,898.8 ns / 3,656 B. The scalar control is still fast (265.9 ns / 80 B); its small timing change is not treated as a new optimization claim. Full before/after tables follow below.

## Deferred work

Follow-up: direct scalar arrays/lists and collection-valued actions are now implemented and measured in [the collection audit](COLLECTION_CODEGEN_AUDIT.md). The table below records the original deferrals at the end of the scalar expansion.

Action-only scaffolds and inherited entry points are subsequently implemented in [the entry-point audit](SHAPE_CODEGEN_AUDIT.md).

| Area | Current behavior and reason for deferral |
| --- | --- |
| Direct collection parsing | Lists/arrays work through the generated shared parser. Moving them to the direct parser requires parity for comma-separated values, multiple tokens, duplicates, element errors, and hook parser state; simply broadening type eligibility would be incorrect. |
| Rich action lifecycles | Collections, custom object types, prompting/conditional requirements, sticky values and custom hooks combined with actions can still force reflection. Generated setters and invokers need integration with the full action lifecycle before removing these guards. |
| More scaffold shapes | Propertyless action-only scaffolds are not discovered by the partial-property trigger. Generic/nested-generic scaffolds, legacy property-based action conventions, inherited action methods, and special accessor shapes need a separate generator-shape compatibility pass. Keep these on ordinary classic scaffolds rather than assuming eligibility from individual feature bullets. |
| Compile-time feedback | The new runtime tier API explains what ran, but generator diagnostics with specific fallback reasons would make adoption easier. Diagnostics and generator-driver tests should be designed together to avoid noisy warnings for intentional classic models. |
| Removing remaining reflection | Component-model conversions, custom reviver discovery/invocation, validators, usage metadata and shared lifecycle operations still contain reflection. Compile-time converter delegates and reviver registration need extensibility/AOT tests; this release does not claim zero reflection or Native AOT safety. |
| Larger performance study | Benchmark cold starts, large option/action sets, shared-parser models, and Linux/macOS separately. The measured two-option workloads should not be extrapolated into universal speedups. |

The priority was to move common models onto the existing direct parser, repair observable compatibility gaps, and retain proven classic semantics for the more complex features.

## Before: d2219e3

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i9-14900HX 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
| Method     | Scenario         | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |----------------- |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Reflection** | **Main with helper** | **2,774.8 ns** |   **922.30 ns** | **50.55 ns** |  **1.00** |    **0.02** | **0.1755** |    **3321 B** |        **1.00** |
| Generated  | Main with helper | 2,768.0 ns |    76.89 ns |  4.21 ns |  1.00 |    0.02 | 0.1755 |    3321 B |        1.00 |
|            |                  |            |             |          |       |         |        |           |             |
| **Reflection** | **Nullable options** | **6,898.8 ns** | **1,197.25 ns** | **65.63 ns** |  **1.00** |    **0.01** | **0.1907** |    **3656 B** |        **1.00** |
| Generated  | Nullable options | 7,200.2 ns |   713.82 ns | 39.13 ns |  1.04 |    0.01 | 0.2975 |    5632 B |        1.54 |
|            |                  |            |             |          |       |         |        |           |             |
| **Reflection** | **Scalar control**   | **2,631.0 ns** |   **475.58 ns** | **26.07 ns** |  **1.00** |    **0.01** | **0.1602** |    **3065 B** |        **1.00** |
| Generated  | Scalar control   |   294.4 ns |   447.96 ns | 24.55 ns |  0.11 |    0.01 | 0.0038 |      80 B |        0.03 |

## After: final implementation

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i9-14900HX 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
| Method     | Scenario         | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |----------------- |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Reflection** | **Main with helper** | **2,779.7 ns** | **1,061.64 ns** | **58.19 ns** |  **1.00** |    **0.03** | **0.1755** |    **3321 B** |        **1.00** |
| Generated  | Main with helper |   348.3 ns |    93.57 ns |  5.13 ns |  0.13 |    0.00 | 0.0138 |     264 B |        0.08 |
|            |                  |            |             |          |       |         |        |           |             |
| **Reflection** | **Nullable options** | **2,810.5 ns** | **1,462.19 ns** | **80.15 ns** |  **1.00** |    **0.04** | **0.1297** |    **2504 B** |        **1.00** |
| Generated  | Nullable options |   502.8 ns |    78.50 ns |  4.30 ns |  0.18 |    0.00 | 0.0038 |      88 B |        0.04 |
|            |                  |            |             |          |       |         |        |           |             |
| **Reflection** | **Scalar control**   | **2,607.3 ns** |   **464.23 ns** | **25.45 ns** |  **1.00** |    **0.01** | **0.1602** |    **3065 B** |        **1.00** |
| Generated  | Scalar control   |   265.9 ns |    76.98 ns |  4.22 ns |  0.10 |    0.00 | 0.0038 |      80 B |        0.03 |
