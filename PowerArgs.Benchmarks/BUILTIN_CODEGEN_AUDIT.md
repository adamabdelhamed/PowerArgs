# Native built-in rules audit - October 22, 2026

Baseline: `484a83e`. The same new `BuiltinBenchmarks` source was run before and after the runtime/generator changes.

## Delivered

- Non-prompting conditional `ArgRequired` and exact `ArgCantBeCombinedWith` now select the direct generated parser for supported scalars and arrays/lists, including global options, action parameters, and action-object properties. They evaluate after population against live generated definition values, without installing conditional hooks or traversing the general hook lifecycle.
- Conditional expressions are parsed lazily once per argument definition and refreshed when their text changes. Exclusions reuse the expression already created by their attribute. A definition-owned resolver eliminates per-evaluation resolver/closure allocation. The existing boolean evaluator preserves grouping, negation, short circuits, alias/case behavior, and errors. Explicit false, zero, defaults, and non-null collections count as present, just as in classic PowerArgs.
- Range, regex, existing-file, and existing-directory validators reuse their existing implementations through a prepared, stable priority order. Metadata replacement and priority edits refresh the plan. This removes repeated ordering work without duplicating string rewriting, normalization, or error contracts. Exact built-in validators now work with the typed array/list converters; custom collection validators retain the shared parser. As in classic parsing, collection validation applies to the first raw value, not each element.
- Help dispatch calls the built-in lifecycle methods directly rather than building/sorting general hook sequences. Omitted help targets still receive the classic boolean-type check. Consuming help cancellation state fixes duplicate usage output when a definition with multiple help flags is reused; that fix also benefits classic parsing.
- Direct parsing now honors ValidationEnabled for required/scalar checks. Exclusion hooks still execute when validation is disabled, matching the shared lifecycle.
- Concurrent cold-start tests exposed a shared mutable boolean tokenizer. Its buffer is now thread-local, fixing corrupted conditions for both classic and generated callers. Prepared rule state belongs to the per-thread definition, never to process-wide generated schemas.

These are native integration and dispatch improvements, not wholesale rewrites of scalar validators. No application changes are needed beyond opting into generated scaffolds. Interactive hooks continue to use the shared lifecycle.

## Validation

`dotnet test PowerArgsTestCore -c Release --no-restore` passed **536 tests**, zero failures or skips (32 cases added to the previous 504). Paired generated/classic cases check conditional choices, defaults and false flags, priority/error ordering, malformed and unknown expressions, short circuits, case-sensitive aliases, raw collection validation, path normalization, action/global conditions, unselected actions, repeated help cancellation, omitted invalid help targets, supplied metadata edits, disabled validation, and conservative fallback. Parallel cold-start/repeated-failure coverage exercises definition isolation and the tokenizer fix.

The tests compare values, exception types/messages, and inner exception types where applicable. They assert the generated tier, not just IsGenerated. This is not a proof of identical diagnostic precedence for every combination of malformed command-line syntax and multiple independent validation failures.

## Benchmarks

Command: `dotnet run -c Release --project PowerArgs.Benchmarks -- --builtins-only`.

BenchmarkDotNet 0.15.8 ShortRun: one launch, three warmups, three measured iterations. Windows 11, Intel Core i9-14900HX, .NET SDK 10.0.301, .NET runtime 10.0.9, x64 RyuJIT. All eight before and final-after cases completed. Builds/tests were not run concurrently with benchmark execution. These are warm steady-state measurements; first-use generation registration, definition construction, expression parsing, and validator-plan preparation are not measured separately. Full short-run confidence intervals are retained below, so ratios describe these workload means, not universal guarantees.

| Workload | Previous generated | New generated | Current classic | Faster than previous generated / classic | New allocation / classic |
| --- | ---: | ---: | ---: | ---: | ---: |
| Conditional required | 4,407.0 ns | 285.0 ns | 5,445.2 ns | 15.5x / 19.1x | 56 / 7,568 B |
| Exclusion | 4,336.3 ns | 225.6 ns | 7,468.5 ns | 19.2x / 33.1x | 32 / 9,992 B |
| Validated array | 2,260.9 ns | 389.4 ns | 2,266.0 ns | 5.8x / 5.8x | 192 / 2,808 B |
| Help false plus range validation | 814.5 ns | 374.2 ns | 8,438.4 ns | 2.2x / 22.6x | 152 / 10,401 B |

Allocation falls by roughly 93-99.7% versus classic on these workloads. The help workload passes `-Help false`, so it measures dispatch and scalar validation, not usage rendering or console I/O. The array workload contains three integer tokens with a range validator. File-system validators are covered by parity tests, not performance claims. Conditional/exclusion gains primarily come from leaving the shared lifecycle, avoiding repeated expression/hook setup, and reusing generated definitions.

### Before: complete summary

| Method    | Scenario             | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------- |--------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Classic   | Conditional          | 5,263.4 ns |   180.8 ns |   9.91 ns |  1.00 |    0.00 | 0.3967 |    7568 B |        1.00 |
| Generated | Conditional          | 4,407.0 ns |   819.4 ns |  44.91 ns |  0.84 |    0.01 | 0.3738 |    7096 B |        0.94 |
|           |                      |            |            |           |       |         |        |           |             |
| Classic   | Exclusion            | 7,598.3 ns | 4,043.4 ns | 221.63 ns |  1.00 |    0.04 | 0.5264 |    9968 B |        1.00 |
| Generated | Exclusion            | 4,336.3 ns |   242.0 ns |  13.27 ns |  0.57 |    0.01 | 0.4425 |    8392 B |        0.84 |
|           |                      |            |            |           |       |         |        |           |             |
| Classic   | Help false           | 8,439.5 ns | 1,197.6 ns |  65.65 ns |  1.00 |    0.01 | 0.5493 |   10377 B |        1.00 |
| Generated | Help false           |   814.5 ns |   300.2 ns |  16.45 ns |  0.10 |    0.00 | 0.0515 |     984 B |        0.09 |
|           |                      |            |            |           |       |         |        |           |             |
| Classic   | Validated collection | 2,375.0 ns | 2,085.5 ns | 114.31 ns |  1.00 |    0.06 | 0.1488 |    2808 B |        1.00 |
| Generated | Validated collection | 2,260.9 ns | 2,376.1 ns | 130.24 ns |  0.95 |    0.06 | 0.1488 |    2808 B |        1.00 |

### After: complete summary

| Method    | Scenario             | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------- |--------------------- |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| Classic   | Conditional          | 5,445.2 ns |   995.79 ns |  54.58 ns |  1.00 |    0.01 | 0.3967 |    7568 B |       1.000 |
| Generated | Conditional          |   285.0 ns |   185.64 ns |  10.18 ns |  0.05 |    0.00 | 0.0029 |      56 B |       0.007 |
|           |                      |            |             |           |       |         |        |           |             |
| Classic   | Exclusion            | 7,468.5 ns | 1,993.90 ns | 109.29 ns |  1.00 |    0.02 | 0.5188 |    9992 B |       1.000 |
| Generated | Exclusion            |   225.6 ns |   165.48 ns |   9.07 ns |  0.03 |    0.00 | 0.0017 |      32 B |       0.003 |
|           |                      |            |             |           |       |         |        |           |             |
| Classic   | Help false           | 8,438.4 ns |   781.84 ns |  42.86 ns |  1.00 |    0.01 | 0.5493 |   10401 B |        1.00 |
| Generated | Help false           |   374.2 ns |   103.82 ns |   5.69 ns |  0.04 |    0.00 | 0.0076 |     152 B |        0.01 |
|           |                      |            |             |           |       |         |        |           |             |
| Classic   | Validated collection | 2,266.0 ns |   650.09 ns |  35.63 ns |  1.00 |    0.02 | 0.1488 |    2808 B |        1.00 |
| Generated | Validated collection |   389.4 ns |    69.50 ns |   3.81 ns |  0.17 |    0.00 | 0.0100 |     192 B |        0.07 |

## Deferred

- PromptIfEmpty, prompting ArgRequired, StickyArg, and TabCompletion/REPL stay on the shared lifecycle. Their console/persistence interactions require dedicated interactive and state-lifecycle validation; optimizing warm in-memory parsing has greater benefit in this pass.
- Range still validates raw text before normal revival, and regex uses the existing regex implementation. A typed post-revival validator or generated regex specialization could remove more work, but must preserve custom revivers, cultures, raw-value errors, metadata edits, and validation order. Duplicating those contracts without measured need would add maintenance risk.
- Custom collection validators and arbitrary user hooks remain on the shared lifecycle because they may inspect parser additional-value state, mutate values, or depend on unsupported stages. Existing custom scalar validator behavior is retained.
- Cold-start/AOT/trim analysis, custom/nested action value types, generic scaffolds, broader async ambient-context changes, and a complete audit of all multi-error diagnostic ordering remain separate work. This change is not a general Native AOT guarantee.
