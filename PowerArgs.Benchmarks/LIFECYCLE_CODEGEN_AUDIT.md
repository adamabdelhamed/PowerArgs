# Generated action lifecycle audit — October 22, 2026

Baseline: `3e00301`. Actions with root hooks, conditional requirements, or collection validators previously forced full reflection even when their signatures and value types were supported by the generator.

## Delivered

- These actions can now select the generated shared-parser tier. Metadata, factories, action-object setters, and invocation delegates are emitted at compile time; ParseInternal retains the existing parsing, validation, hook ordering, and cancellation lifecycle.
- Generated action specs are bound to their definition actions at definition construction. ParseAction results retain generated invocation, action arguments/parameters, and caller-supplied definitions. Absent action-object values are cleared like classic mapping; omitted value-type method arguments invoke with default values while the public shared-parser ActionParameters array preserves null entries.
- Reusable shared generated definitions are cached per thread using the existing reflection reuse rule. Custom hooks still get fresh definitions. Reuse eligibility is checked again because conditional requirements can install hooks during validation. Success/failure/omission tests verify cleanup.
- ArgAction.InvokeAsync and the definition-based Args.InvokeActionAsync overload await generated Task delegates instead of synchronously blocking. Generated synchronous and asynchronous action errors carry their parse context. Other async facade methods and thread-local ambient context have not been redesigned.
- Nullable reference annotations no longer produce illegal typeof expressions or nullable constructor expressions. Nullable properties, scalar collections, and action-object annotations compile and parse.
- Fixed a classic invocation-cache bug exposed by parity testing: a null argument for a value-type parameter now maps to default(T), matching MethodInfo.Invoke, rather than throwing during expression-tree unboxing.
- External action types/resolvers and legacy Action selector properties explicitly retain reflection rather than silently losing their discovery behavior.

## Validation

`dotnet test PowerArgsTestCore/PowerArgsTestCore.csproj -c Release` passed all 504 tests with no failures or skips. Eleven new cases cover lifecycle order, hook rewriting/cancellation/freshness, collection validators, conditional requirements, defaults, supplied-definition changes, deferred invocation, omitted parameters, async completion and failures, nullable reference declarations, shared-definition cleanup, and external/legacy fallbacks. The async gate tests check completion ordering with bounded cleanup; they are not performance thresholds.

## Benchmarks

Run `dotnet run -c Release --project PowerArgs.Benchmarks -- --lifecycle-only`. Identical benchmark source was run against the baseline and updated runtime. All six cases completed in BenchmarkDotNet ShortRun jobs (one launch, three warmups, three measurement iterations). Builds/tests did not run concurrently with benchmark processes. Final eligibility guards and nullable-constructor formatting do not alter these benchmark paths.

The hook workload has one definition hook and a scalar action; the validator workload has a three-element array on an action object; the control is an already-direct scalar action. These are warm steady-state Windows/.NET 10 measurements. Full confidence intervals are preserved below; especially the hook's classic measurements and validator's generated measurements have wide short-run intervals.

| Scenario | Previous generated mean | New generated mean | Current classic mean | New / classic allocation |
| --- | ---: | ---: | ---: | ---: |
| Definition hook | 20,362.4 ns | 6,537.6 ns | 25,899.9 ns | 11,459 / 18,762 B |
| Collection validator | 4,116.9 ns | 3,939.7 ns | 4,402.6 ns | 4,704 / 5,064 B |
| Direct control | 284.8 ns | 277.4 ns | 3,095.2 ns | 392 / 4,600 B |

The hook workload's generated mean is about 3.1x faster than the prior generated model (which used reflection), and about 4x faster than current classic, with 39% less allocation. The collection-validator workload allocates 7% less than classic; its timing difference is inconclusive in this short run. The direct control retains 392 B allocation, with no new timing improvement claimed. An initial implementation rebuilt shared generated definitions on every call and regressed the validator workload; adding safe reuse removed that measured regression.

## Remaining work and limits

- Custom/nested action value types, action-object type-level lifecycle metadata, external/resolved actions, generic scaffolds, and optional/ref/generic method signatures need further generator mapping and parity work. They remain guarded rather than being routed through an incomplete direct implementation.
- Hooks that require reflected Source/PropertyInfo/MethodInfo cannot assume those exist on generated definitions. Metadata-based hooks are the supported contract tested here; arbitrary reflection-dependent hooks need a separate adapter design.
- Interactive prompting, sticky persistence, and REPL combinations use the shared lifecycle where eligible, but this expansion did not manually exercise interactive consoles. Automated conditional/validation/hook tests do not establish full interactive acceptance.
- Shared parsing still allocates parser/hook objects and may reflect during conversion. More caching must respect mutable metadata and hook state. Generator fallback diagnostics, typed element converters, cold starts, cross-platform performance, and AOT remain separate work.
- Async ambient state remains thread-local and the generic async facade retains its existing scheduling behavior. This change fixes generated action awaiting and exception context without claiming a complete async-context redesign.

## Before: 3e00301

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i9-14900HX 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
| Method     | Scenario             | Mean        | Error        | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |--------------------- |------------:|-------------:|------------:|------:|--------:|-------:|----------:|------------:|
| **Reflection** | **Collection validator** |  **4,432.4 ns** |  **5,592.09 ns** |   **306.52 ns** |  **1.00** |    **0.08** | **0.2594** |    **4920 B** |        **1.00** |
| Generated  | Collection validator |  4,116.9 ns |  1,082.87 ns |    59.36 ns |  0.93 |    0.06 | 0.2594 |    4920 B |        1.00 |
|            |                      |             |              |             |       |         |        |           |             |
| **Reflection** | **Definition hook**      | **21,880.5 ns** | **27,482.56 ns** | **1,506.41 ns** |  **1.00** |    **0.09** | **0.9766** |   **18594 B** |        **1.00** |
| Generated  | Definition hook      | 20,362.4 ns | 35,450.11 ns | 1,943.14 ns |  0.93 |    0.10 | 0.9766 |   18594 B |        1.00 |
|            |                      |             |              |             |       |         |        |           |             |
| **Reflection** | **Direct control**       |  **2,982.1 ns** |    **223.90 ns** |    **12.27 ns** |  **1.00** |    **0.01** | **0.2365** |    **4456 B** |        **1.00** |
| Generated  | Direct control       |    284.8 ns |      9.19 ns |     0.50 ns |  0.10 |    0.00 | 0.0205 |     392 B |        0.09 |

## After: generated shared actions

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i9-14900HX 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
| Method     | Scenario             | Mean        | Error        | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |--------------------- |------------:|-------------:|------------:|------:|--------:|-------:|----------:|------------:|
| **Reflection** | **Collection validator** |  **4,402.6 ns** |    **309.09 ns** |    **16.94 ns** |  **1.00** |    **0.00** | **0.2670** |    **5064 B** |        **1.00** |
| Generated  | Collection validator |  3,939.7 ns |  4,399.65 ns |   241.16 ns |  0.89 |    0.05 | 0.2480 |    4704 B |        0.93 |
|            |                      |             |              |             |       |         |        |           |             |
| **Reflection** | **Definition hook**      | **25,899.9 ns** | **33,972.77 ns** | **1,862.16 ns** |  **1.00** |    **0.09** | **0.9766** |   **18762 B** |        **1.00** |
| Generated  | Definition hook      |  6,537.6 ns |  1,352.05 ns |    74.11 ns |  0.25 |    0.02 | 0.5951 |   11459 B |        0.61 |
|            |                      |             |              |             |       |         |        |           |             |
| **Reflection** | **Direct control**       |  **3,095.2 ns** |    **468.39 ns** |    **25.67 ns** |  **1.00** |    **0.01** | **0.2441** |    **4600 B** |        **1.00** |
| Generated  | Direct control       |    277.4 ns |     53.51 ns |     2.93 ns |  0.09 |    0.00 | 0.0205 |     392 B |        0.09 |

## Built-in follow-up

Non-prompting conditional requirements, built-in exclusions, and built-in collection validators now use the direct parser where their value types are supported. See [the built-in rules audit](BUILTIN_CODEGEN_AUDIT.md) for the follow-up measurements and compatibility coverage. Custom lifecycle workloads in this report retain the shared parser.
