# PowerArgs development notes

## Generated argument models

- The source-generated pipeline is opt-in by shape: the scaffold class and its declared public properties are `partial`, with property bodies supplied by `PowerArgs.Generator`. A partial class with public Main/action entry points (including inherited instance methods) can also opt in with zero properties. Ordinary classes and unrelated partial classes stay on reflection.
- Generated models have two compile-time-selected tiers. Supported scalar and scalar-collection models use `GeneratedArgumentParser`; feature-rich models still generate their definition, construction, setters, and main invoker but reuse `Args.ParseInternal` for the mature hook/parser lifecycle. Keep this shared-parser generated tier for prompting and conditional requirements, complex collections, secure arguments, inheritance, custom types/validators/hooks, sticky values, tab completion, unexpected arguments, and other constructible metadata instead of falling all the way back to reflected scaffold mapping.
- Generated invocation supports parameterless `Main` methods and `[ArgActionMethod]` methods returning either `void` or `Task`. Actions may be parameterless, take one action-argument object with supported public properties, or take directly revivable scalar/collection parameters. Action argument metadata, shortcuts, descriptions, positions, supported scalar validation, and case rules are emitted too. Unsupported signatures deliberately fall back to reflection.
- A generated definition must preserve the aliases and validator behavior of the reflection definition. Keep paired core tests for generated and reflection-backed models when expanding attribute coverage.
- Pipeline selection is static: the generator chooses the direct generated parser, the shared-parser generated tier, or full reflection from compile-time shape and metadata. Runtime command-line values must never change that selection; valid legacy syntax is handled on the selected pipeline and invalid input produces compatible errors there.
- For the supported scalar types, generated parsing uses `GeneratedArgumentParser` directly and keeps only a thread-local definition for ambient parse metadata. Inputs or metadata outside that narrow fast-path contract must continue through the existing parser.
- Both generated tiers avoid reflected scaffold discovery, activation, and property assignment. The direct tier additionally avoids the allocation-heavy general hook/alias machinery; the shared-parser tier deliberately retains it for behavioral compatibility. Do not make the full reflection fallback depend on generated code.
- Type-based reflection parsing reuses definitions per thread when they contain no hooks beyond the exact built-in default-value attributes. Keep `CommandLineArgumentsDefinition.Clean()` complete as mutable parse state is added; models containing other or derived hooks deliberately receive fresh reflected definitions.
- Keep performance measurements in `PowerArgs.Benchmarks`; do not turn timing thresholds into unit tests. Pass `--parse-only`, `--invoke-only`, or `--conversion-only` after `--` to isolate a suite and add `--profile` to collect EventPipe CPU traces.
- Nullable scalars and additional numeric/character types may use the direct parser, but must retain classic conversion semantics (including nullable booleans taking values, culture, constants, custom revivers, and errors). Ordinary helper methods are not a reason to fall back. Inherited action properties must preserve reflection ordering and collapse overrides before assigning aliases.
- Runtime custom scalar revivers override built-in direct conversions, including after a model has been used. Nullable type unwrapping is cached to avoid repeated generic type construction. Keep enum revival on its existing alias/case contract.
- Use `GeneratedArgumentModelRegistry.GetPipelineKind` to distinguish direct, shared-parser, and reflection tiers. `--expansion-only` benchmarks the nullable/helper eligibility improvements and a scalar control; `PowerArgs.Benchmarks/CODEGEN_AUDIT.md` records evidence and deferred compatibility work.
- Direct collection eligibility is limited to rank-one arrays and exact `List<T>` with direct scalar elements. Emit typed converters, preserve custom element revivers, and keep nested/custom collections and collection validators/hooks on the established lifecycle. Additional values are spans over input, not extra parser-state lists. Preserve classic comma-versus-token semantics, negative numbers, slash boundaries, and explicit long-form aliases; pair collection tests with classic models. `--collections-only` and `PowerArgs.Benchmarks/COLLECTION_CODEGEN_AUDIT.md` record this expansion.
- Method discovery must match public reflection lookup: derived methods first, overrides replace their virtual slot, hidden base methods remain, and base static/non-public methods are excluded. Invoke an inherited method through its declaring type so C# hiding does not change dispatch. Respect AttributeUsage inheritance and multiplicity for method/type metadata, and preserve inherited hooks by selecting the shared lifecycle. Keep empty action-object construction distinct from parameterless actions. `GeneratedShapeTests` and `--shapes-only` cover these contracts; evidence is in `PowerArgs.Benchmarks/SHAPE_CODEGEN_AUDIT.md`.

## Help template language

- `DocumentToken` uses a document-specific linear scanner. Keep it instance-free and thread-safe; the general `Tokenizer<T>` is still used by object-path parsing and must retain growable per-instance buffers without process-wide string interning.
- Template parsing validates and compiles nested bodies eagerly. Invalid closing tags must fail even inside a branch that is false at render time, and closing tag names must match their opener.
- `DocumentRenderer` caches compiled templates by template text and source location while the built-in expression set is unchanged. Registering or unregistering an expression provider disables compiled-document reuse for that renderer, preserving the legacy contract that custom providers create a fresh expression per render.
- Built-in document expressions append to a shared render buffer to avoid quadratic immutable `ConsoleString` concatenation. Custom `IDocumentExpression` implementations remain compatible through the public `Evaluate` fallback.
- Durable template coverage lives under `PowerArgsTestCore\Templating`. Run template benchmarks with `dotnet run -c Release --project PowerArgs.Benchmarks -- --template-only`; add `--profile` for EventPipe traces.

## General tokenizer

- Match multi-character delimiters only at their source start, longest eligible match first. Do not reintroduce suffix matching against decoded token text: escaped characters must never become delimiters after decoding.
- Delimiter lookahead must preserve quoted-literal and escape handling throughout the candidate, including an escape indicator at its end whose next character lies outside the candidate. Whitespace-containing delimiters require `WhitespaceBehavior.Include`.
- Keep regressions for overlapping delimiters, quote/escape precedence (including custom and disabled escapes), and source positions in `PowerArgsTestCore/Templating/TokenizerTests.cs`.

## Console table wrapping

- Normalize smart-wrap widths to at least one before layout, for both console-derived and explicitly configured widths. Headless consoles can report zero, and preceding columns can exhaust even a positive console width; each wrapping iteration must still consume input.
- Keep zero/narrow-console and explicit-width regressions in `ConsoleTableBuilderTests`, checking preserved content and restoring shared console state after each test.

## Custom command-line names

- `ArgShortcut` accepts long names as well as short flags. Combine `ArgShortcutPolicy.ShortcutsOnly` with the desired aliases to replace a CLR property or action method name in parsing and help; no separate display-name attribute is needed. Keep parsing/invocation and help-output coverage together in `ShortcutTests`.
