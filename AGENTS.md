# PowerArgs development notes

## Generated argument models

- The source-generated pipeline is opt-in by shape: the scaffold class and its public argument properties are declared `partial`, and the argument properties are partial properties whose bodies are supplied by `PowerArgs.Generator`.
- Generated metadata currently supports `[ArgRequired]`, `[ArgShortcut]`, `[ArgPosition]`, `[ArgDescription]`, `[ArgIgnore]`, `[ArgIgnoreCase]`/`[ArgEnforceCase]`, and `[ArgRange]`. If a class or property uses other PowerArgs metadata, the generator still implements the partial properties but does not register a generated model; the existing reflection pipeline remains authoritative.
- Generated invocation supports parameterless `Main` methods and `[ArgActionMethod]` methods returning either `void` or `Task`. Actions may be parameterless, take one action-argument object with supported public scalar properties, or take multiple directly revivable scalar parameters. Action argument metadata, shortcuts, descriptions, positions, validation, and case rules are emitted too. Unsupported signatures deliberately fall back to reflection.
- A generated definition must preserve the aliases and validator behavior of the reflection definition. Keep paired core tests for generated and reflection-backed models when expanding attribute coverage.
- Pipeline selection is static: the generator either registers the supported model or leaves the entire model on reflection. Runtime command-line values must never cause a registered generated model to switch to reflection; valid legacy syntax is handled by the generated parser and invalid input produces compatible errors on that same pipeline.
- For the supported scalar types, generated parsing uses `GeneratedArgumentParser` directly and keeps only a thread-local definition for ambient parse metadata. Inputs or metadata outside that narrow fast-path contract must continue through the existing parser.
- Generated parsing avoids reflected scaffold discovery, activation, property assignment, and the allocation-heavy hook/alias machinery. Do not make the fallback path depend on generated code.
- Keep performance measurements in `PowerArgs.Benchmarks`; do not turn timing thresholds into unit tests. Pass `--invoke-only` after `--` for the invocation suite and add `--profile` to collect EventPipe CPU traces.
