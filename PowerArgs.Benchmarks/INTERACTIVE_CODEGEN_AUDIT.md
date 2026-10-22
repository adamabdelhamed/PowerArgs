# Interactive generated compatibility audit - October 22, 2026

Baseline: `bec869a`. This pass closes concrete shared-lifecycle correctness gaps and adds a reusable classic/generated comparison harness. Interactive scaffolds continue to use generated definitions, factories, setters, and invokers with the shared parser.

## Reproduced and fixed

Two regression tests were run before the production fix. Both failed: conditional prompting left a generated root Name null despite entering "alice", and classic action-object prompting threw InvalidArgDefinitionException because it tried to assign a PromptPayload property on the parent action scaffold.

- Added `ArgHook.HookContext.PopulateCurrentArgument(rawValue)` for argument-level AfterPopulateProperties hooks. It checks argument ownership, clears the previous revived value, runs normal argument hooks/validation/revival, and immediately applies root values through the generated setter or classic property. Selected action arguments remain in their definition slots until the normal post-hook payload/parameter construction. Conditional required prompting now uses this operation instead of PropertyInfo.SetValue.
- Generated root setters map null to default, matching reflected value-type assignment. This supports clearing previously populated optional values without stale state or unboxing failures.
- Shared argument population now exposes RevivedProperty to AfterPopulateProperty hooks, including late updates. Previously the shared path could leave that documented field null or stale.
- Conditional prompt validation and revival errors stay ArgExceptions rather than being mislabeled as invalid definitions. Malformed conditional definitions retain their existing definition-error handling.
- Unconditional required prompts now run the ordinary value validators that were skipped before input existed. These run once in their established order; validators after Required continue normally. Previously a prompted value could bypass a range or regex validator that ran earlier with null input.
- PromptIfEmpty now respects IsNonInteractive. This prevents unattended prompting, including KeepAsking loops, in both classic and generated models.

The new API is for AfterPopulateProperties. Calling it from a per-argument BeforePopulateProperty/AfterPopulateProperty hook would recursively invoke that hook. It does not supply arbitrary PropertyInfo/MethodInfo metadata and is not an all-purpose replacement for reflection-based extensions.

## Consistency evidence

`dotnet test PowerArgsTestCore -c Release --no-restore --blame-hang-timeout 45s` passed **552 tests**, zero failures or skips, up from 536. The 16 new cases cover:

- Prompted root, action-object, and action-parameter assignment, including ParseAction followed by deferred invocation.
- Explicit values, omitted values, and explicit false presence; validated typed prompts and invalid input.
- Escape as a user error outside REPL and REPL recovery without invoking the cancelled command.
- Generated option-name tab completion, memory-only sticky persistence, and preservation after failed validation.
- Repeated empty prompt retries, noninteractive zero-read guarantees, and supplied-definition reuse after failure/success.
- Late replacement, clearing references/value types, and revived-value observations in custom hooks.
- Validator ordering/rewriting without duplicate application; rejecting missing or foreign argument contexts.

`PipelineParity` now provides a common comparison contract for these scenarios and the previous built-in rule tests. It checks Reflection versus the expected generated tier, projected values, error types/messages/inner exception types, and generated error context. Assertion failures and unexpected exceptions are not swallowed as matching results. Explicit expected-value assertions supplement comparisons so a shared bug cannot silently satisfy parity.

Interactive tests use a bounded synthetic console implementing both ReadKey overloads, restore ConsoleProvider, run nonparallel, disable persistent command history and filesystem completion, and use memory-only sticky storage. This is automated simulated-console coverage, not physical-terminal or cross-platform visual acceptance. It does not establish parity for every plugin or every interactive option combination.

## Performance

All measurements are BenchmarkDotNet ShortRun (one launch, three warmups, three measurement iterations), Windows 11 / .NET 10.0.9, Intel Core i9-14900HX. Tests/builds were not run concurrently with benchmark workloads. These are warm workload measurements, not cold-start or human-input latency claims.

The existing `--builtins-only` workloads retain exactly the previous generated allocations: 56 B conditional, 32 B exclusion, 152 B help-false, and 192 B validated array. Previous/current generated means were 285.0/273.2 ns, 225.6/215.0 ns, 374.2/371.4 ns, and 389.4/415.4 ns respectively. Short-run confidence intervals overlap; no new speedup or significant regression is claimed.

### Existing built-in control suite

| Method    | Scenario             | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------- |--------------------- |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Classic   | Conditional          | 5,354.7 ns |   559.56 ns | 30.67 ns |  1.00 |    0.01 | 0.3967 |    7568 B |       1.000 |
| Generated | Conditional          |   273.2 ns |    17.51 ns |  0.96 ns |  0.05 |    0.00 | 0.0029 |      56 B |       0.007 |
|           |                      |            |             |          |       |         |        |           |             |
| Classic   | Exclusion            | 7,432.5 ns | 1,608.88 ns | 88.19 ns |  1.00 |    0.01 | 0.5264 |    9992 B |       1.000 |
| Generated | Exclusion            |   215.0 ns |    46.68 ns |  2.56 ns |  0.03 |    0.00 | 0.0014 |      32 B |       0.003 |
|           |                      |            |             |          |       |         |        |           |             |
| Classic   | Help false           | 8,359.0 ns |   502.63 ns | 27.55 ns |  1.00 |    0.00 | 0.5493 |   10401 B |        1.00 |
| Generated | Help false           |   371.4 ns |    61.83 ns |  3.39 ns |  0.04 |    0.00 | 0.0076 |     152 B |        0.01 |
|           |                      |            |             |          |       |         |        |           |             |
| Classic   | Validated collection | 2,289.2 ns |   521.72 ns | 28.60 ns |  1.00 |    0.02 | 0.1488 |    2808 B |        1.00 |
| Generated | Validated collection |   415.4 ns |   137.16 ns |  7.52 ns |  0.18 |    0.00 | 0.0100 |     192 B |        0.07 |

### Late-update workload

`dotnet run -c Release --project PowerArgs.Benchmarks -- --hook-updates-only` measures a range-validated integer updated by an AfterPopulateProperties hook through the new API, plus an already-direct scalar control. Setup asserts the actual updated value and pipeline tier. There is no before/after speedup claim for late population: the old generated path did not correctly perform the prompted assignment, and the new public API did not exist. This benchmark isolates in-memory lifecycle/assignment costs from console I/O.

| Method    | Scenario       | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------- |--------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Classic   | Direct control | 1,581.1 ns |   374.5 ns |  20.53 ns |  1.00 |    0.02 | 0.1316 |    2488 B |        1.00 |
| Generated | Direct control |   151.3 ns |   103.9 ns |   5.69 ns |  0.10 |    0.00 | 0.0024 |      48 B |        0.02 |
|           |                |            |            |           |       |         |        |           |             |
| Classic   | Late update    | 6,833.7 ns | 2,207.1 ns | 120.98 ns |  1.00 |    0.02 | 0.4807 |    9177 B |        1.00 |
| Generated | Late update    | 3,900.2 ns | 1,365.9 ns |  74.87 ns |  0.57 |    0.01 | 0.3662 |    6984 B |        0.76 |

## Remaining work

- Broaden generated action support for custom value types, nested/custom collections, optional parameters, and more scaffold shapes. Those capabilities were not expanded in this pass.
- Adapt extension hooks that require arbitrary reflection metadata. PopulateCurrentArgument addresses late value population, not general member inspection.
- Grow the shared parity harness across the rest of the classic suite, especially mixed syntax/validation error precedence and third-party lifecycle combinations.
- Interactive features still use the shared lifecycle. Moving them to the direct parser requires more complete stage semantics and offers less benefit than closing remaining capability gaps first.
- Generic scaffolds, external/legacy action discovery, async ambient context, trimming/AOT, and physical-terminal/platform acceptance remain separate work.
