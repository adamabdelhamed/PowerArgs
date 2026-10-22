using System;

namespace PowerArgs
{
    internal static class GeneratedArgumentParser
    {
        internal static bool TryParse(GeneratedArgumentModel model, string[] input, CommandLineArgumentsDefinition definition, out object? value)
            => TryParse(model, input, 0, 0, definition, out value);

        internal static bool TryParse(GeneratedArgumentModel model, string[] input, int inputOffset, int firstPosition, CommandLineArgumentsDefinition definition, out object? value)
        {
            var specs = model.DirectArguments;
            if (specs == null)
            {
                throw new InvalidOperationException("The generated model does not contain a direct parser schema");
            }

            Span<byte> specified = specs.Length <= 128 ? stackalloc byte[specs.Length] : new byte[specs.Length];
            var instance = model.Factory();
            if (ArgHook.HookContext.Current != null && RequiresHookProcessing(specs)) ArgHook.HookContext.Current.Args = instance;
            var nextPosition = firstPosition;
            var foundNamedArgument = false;

            for (var inputIndex = inputOffset; inputIndex < input.Length; inputIndex++)
            {
                var token = input[inputIndex];
                var isNamed = IsNamedToken(token, specs, Array.Empty<GeneratedArgumentSpec>(), nextPosition);
                if (isNamed == false)
                {
                    if (foundNamedArgument) throw new UnexpectedArgException("Unexpected argument: " + token);

                    var positionalArgumentIndex = FindPositionedArgument(specs, nextPosition++);
                    if (positionalArgumentIndex < 0) throw new UnexpectedArgException("Unexpected unnamed argument: " + token);
                    if (specified[positionalArgumentIndex] != 0) throw new DuplicateArgException("Argument specified more than once: " + specs[positionalArgumentIndex].Name);

                    var positionalSpec = specs[positionalArgumentIndex];
                    var additionalValues = ReadCollectionTail(positionalSpec, input, ref inputIndex);
                    if (positionalSpec.CollectionReviver != null) foundNamedArgument = true;
                    PopulateArgument(model, definition, instance, positionalSpec, positionalArgumentIndex, token, specified, additionalValues);
                    continue;
                }

                foundNamedArgument = true;

                ReadNamedToken(token, out var key, out var argumentValue, out var hasInlineValue);

                var argumentIndex = FindNamedArgument(specs, key, token, hasInlineValue);
                if (argumentIndex < 0) ThrowUnknownArgument(specs, specified, key);
                if (specified[argumentIndex] != 0) throw new DuplicateArgException("Argument specified more than once: " + specs[argumentIndex].Name);

                var spec = specs[argumentIndex];
                if (token[0] == '-' && spec.IsBoolean)
                {
                    if (inputIndex + 1 < input.Length && IsExplicitBooleanValue(input[inputIndex + 1]))
                    {
                        argumentValue = input[++inputIndex];
                    }
                    else
                    {
                        argumentValue = "true";
                    }
                }
                else if (token[0] == '-' && spec.IsBoolean == false && hasInlineValue == false)
                {
                    argumentValue = inputIndex + 1 < input.Length ? input[++inputIndex] : string.Empty;
                }

                var collectionTail = token[0] == '-' ? ReadCollectionTail(spec, input, ref inputIndex) : default;
                PopulateArgument(model, definition, instance, spec, argumentIndex, argumentValue, specified, collectionTail);
            }

            ApplyDefaults(model, definition, instance, specs, specified);
            ValidateRequired(specs, specified);

            value = instance;
            return true;
        }

        internal static bool TryParseAction(GeneratedArgumentModel model, GeneratedActionSpec action, string[] input, CommandLineArgumentsDefinition definition, CommandLineAction actionDefinition, out object? value, out object? actionArguments)
        {
            var globalSpecs = model.DirectArguments;
            var actionSpecs = action.Arguments;
            if (globalSpecs == null)
            {
                throw new InvalidOperationException("The generated model does not contain a direct parser schema");
            }

            Span<byte> globalSpecified = globalSpecs.Length <= 128 ? stackalloc byte[globalSpecs.Length] : new byte[globalSpecs.Length];
            Span<byte> actionSpecified = actionSpecs.Length <= 128 ? stackalloc byte[actionSpecs.Length] : new byte[actionSpecs.Length];
            var parent = model.Factory();
            if (ArgHook.HookContext.Current != null) ArgHook.HookContext.Current.Args = parent;
            var actionValues = action.ArgumentFactory?.Invoke();
            var nextPosition = 1;
            var foundNamedArgument = false;

            for (var inputIndex = 1; inputIndex < input.Length; inputIndex++)
            {
                var token = input[inputIndex];
                var isNamed = IsNamedToken(token, globalSpecs, actionSpecs, nextPosition);
                if (isNamed == false)
                {
                    if (foundNamedArgument) throw new UnexpectedArgException("Unexpected argument: " + token);

                    var positionedGlobalIndex = FindPositionedArgument(globalSpecs, nextPosition);
                    var positionedActionIndex = FindPositionedArgument(actionSpecs, nextPosition++);
                    if (positionedGlobalIndex < 0 && positionedActionIndex < 0) throw new UnexpectedArgException("Unexpected unnamed argument: " + token);
                    if (positionedGlobalIndex >= 0 && positionedActionIndex >= 0) throw new InvalidArgDefinitionException("Multiple arguments use position " + (nextPosition - 1));
                    if (positionedGlobalIndex >= 0 && globalSpecified[positionedGlobalIndex] != 0) throw new DuplicateArgException("Argument specified more than once: " + globalSpecs[positionedGlobalIndex].Name);
                    if (positionedActionIndex >= 0 && actionSpecified[positionedActionIndex] != 0) throw new DuplicateArgException("Argument specified more than once: " + actionSpecs[positionedActionIndex].Name);
                    var positionedSpec = positionedGlobalIndex >= 0 ? globalSpecs[positionedGlobalIndex] : actionSpecs[positionedActionIndex];
                    var additionalValues = ReadCollectionTail(positionedSpec, input, ref inputIndex);
                    if (positionedSpec.CollectionReviver != null) foundNamedArgument = true;
                    if (positionedGlobalIndex >= 0) PopulateArgument(model, definition, parent, positionedSpec, positionedGlobalIndex, token, globalSpecified, additionalValues);
                    else PopulateActionArgument(action, actionDefinition, actionValues!, positionedSpec, positionedActionIndex, token, actionSpecified, additionalValues);
                    continue;
                }

                foundNamedArgument = true;
                ReadNamedToken(token, out var key, out var argumentValue, out var hasInlineValue);

                var globalIndex = FindNamedArgument(globalSpecs, key, token, hasInlineValue);
                var actionIndex = FindNamedArgument(actionSpecs, key, token, hasInlineValue);
                if (globalIndex < 0 && actionIndex < 0) ThrowUnknownActionArgument(globalSpecs, globalSpecified, actionSpecs, actionSpecified, key);
                if (globalIndex >= 0 && actionIndex >= 0) throw new InvalidArgDefinitionException("Multiple arguments match '" + key.ToString() + "'");
                if (globalIndex >= 0 && globalSpecified[globalIndex] != 0) throw new DuplicateArgException("Argument specified more than once: " + globalSpecs[globalIndex].Name);
                if (actionIndex >= 0 && actionSpecified[actionIndex] != 0) throw new DuplicateArgException("Argument specified more than once: " + actionSpecs[actionIndex].Name);

                var spec = globalIndex >= 0 ? globalSpecs[globalIndex] : actionSpecs[actionIndex];
                if (token[0] == '-' && spec.IsBoolean)
                {
                    if (inputIndex + 1 < input.Length && IsExplicitBooleanValue(input[inputIndex + 1])) argumentValue = input[++inputIndex];
                    else argumentValue = "true";
                }
                else if (token[0] == '-' && spec.IsBoolean == false && hasInlineValue == false)
                {
                    argumentValue = inputIndex + 1 < input.Length ? input[++inputIndex] : string.Empty;
                }

                var collectionTail = token[0] == '-' ? ReadCollectionTail(spec, input, ref inputIndex) : default;
                if (globalIndex >= 0) PopulateArgument(model, definition, parent, spec, globalIndex, argumentValue, globalSpecified, collectionTail);
                else PopulateActionArgument(action, actionDefinition, actionValues!, spec, actionIndex, argumentValue, actionSpecified, collectionTail);
            }

            ApplyDefaults(model, definition, parent, globalSpecs, globalSpecified);
            ApplyActionDefaults(action, actionDefinition, actionValues, actionSpecs, actionSpecified);
            ValidateRequired(globalSpecs, globalSpecified);
            ValidateRequired(actionSpecs, actionSpecified);
            value = parent;
            actionArguments = actionValues;
            return true;
        }

        private static ReadOnlySpan<string> ReadCollectionTail(GeneratedArgumentSpec spec, string[] input, ref int inputIndex)
        {
            if (spec.CollectionReviver == null) return default;
            var start = inputIndex + 1;
            // Preserve classic token boundaries: negative numbers are values, but slash-prefixed
            // tokens after the first value end a collection even when they look like Unix paths.
            while (inputIndex + 1 < input.Length)
            {
                var next = input[inputIndex + 1];
                if (next.StartsWith("/") || ArgParser.IsDashSpecifiedArgumentIdentifier(next)) break;
                inputIndex++;
            }
            return input.AsSpan(start, inputIndex + 1 - start);
        }

        private static void PopulateActionArgument(GeneratedActionSpec action, CommandLineAction definition, object instance, GeneratedArgumentSpec spec, int argumentIndex, string argumentValue, Span<byte> specified, ReadOnlySpan<string> additionalValues = default)
        {
            var argument = definition.Arguments[argumentIndex];
            var revived = ReviveArgument(argument, spec, ref argumentValue, additionalValues);
            specified[argumentIndex] = 1;
            argument.RevivedValue = revived;
            action.SetArgument!(instance, argumentIndex, revived);
        }

        private static void ApplyDefaults(GeneratedArgumentModel model, CommandLineArgumentsDefinition definition, object instance, GeneratedArgumentSpec[] specs, Span<byte> specified)
        {
            for (var i = 0; i < specs.Length; i++)
            {
                if (specified[i] != 0 || definition.Arguments[i].TryGetDefaultValue(out var defaultValue) == false || defaultValue == null) continue;
                PopulateArgument(model, definition, instance, specs[i], i, defaultValue.ToString()!, specified);
            }
        }

        private static void ApplyActionDefaults(GeneratedActionSpec action, CommandLineAction definition, object? instance, GeneratedArgumentSpec[] specs, Span<byte> specified)
        {
            if (instance == null) return;
            for (var i = 0; i < specs.Length; i++)
            {
                if (specified[i] != 0 || definition.Arguments[i].TryGetDefaultValue(out var defaultValue) == false || defaultValue == null) continue;
                PopulateActionArgument(action, definition, instance, specs[i], i, defaultValue.ToString()!, specified);
            }
        }

        private static void ValidateRequired(GeneratedArgumentSpec[] specs, Span<byte> specified)
        {
            for (var i = 0; i < specs.Length; i++)
            {
                if (specs[i].IsRequired && specified[i] == 0) throw new MissingArgException("The argument '" + specs[i].Name + "' is required", new ArgumentNullException(specs[i].Name));
            }
        }

        private static bool IsNamedToken(string token, GeneratedArgumentSpec[] primarySpecs, GeneratedArgumentSpec[] secondarySpecs, int position)
        {
            if (token == "-") throw new ArgException("Missing argument value after '-'");
            if (token.Length > 1 && token[0] == '-') return char.IsDigit(token[1]) == false;
            if (token.Length < 2 || token[0] != '/') return false;

            var hasPositionedArgument = FindPositionedArgument(primarySpecs, position) >= 0 || FindPositionedArgument(secondarySpecs, position) >= 0;
            if (hasPositionedArgument == false) return true;

            var colonIndex = token.IndexOf(':');
            var key = colonIndex < 0 ? token.AsSpan(1) : token.AsSpan(1, colonIndex - 1).Trim();
            var primaryIndex = FindArgument(primarySpecs, key);
            var secondaryIndex = FindArgument(secondarySpecs, key);
            if (primaryIndex >= 0 && primarySpecs[primaryIndex].IsBoolean) return true;
            if (secondaryIndex >= 0 && secondarySpecs[secondaryIndex].IsBoolean) return true;
            if (colonIndex < 1 || colonIndex == token.Length - 1) return false;
            for (var i = 1; i < colonIndex; i++) if (char.IsLetterOrDigit(token[i]) == false) return false;
            return true;
        }

        private static void ReadNamedToken(string token, out ReadOnlySpan<char> key, out string value, out bool hasInlineValue)
        {
            if (token[0] == '/')
            {
                var colonIndex = token.IndexOf(':');
                key = colonIndex < 0 ? token.AsSpan(1) : token.AsSpan(1, colonIndex - 1).Trim();
                value = colonIndex < 0 ? string.Empty : token.Substring(colonIndex + 1).Trim();
                hasInlineValue = colonIndex >= 0;
                if (key.Length == 0) throw new ArgException("Missing argument value after '/'");
                return;
            }

            var content = token.AsSpan(1);
            if (content.Length > 1 && content[0] == '-')
            {
                var equalsIndex = content.IndexOf('=');
                if (equalsIndex > 1)
                {
                    key = content.Slice(1, equalsIndex - 1);
                    value = content.Slice(equalsIndex + 1).ToString();
                    hasInlineValue = true;
                    return;
                }
            }
            key = content;
            value = string.Empty;
            hasInlineValue = false;
        }

        private static void ThrowUnknownArgument(GeneratedArgumentSpec[] specs, Span<byte> specified, ReadOnlySpan<char> key)
        {
            ValidateRequired(specs, specified);
            throw new UnexpectedArgException("Unexpected named argument: " + key.ToString());
        }

        private static void ThrowUnknownActionArgument(GeneratedArgumentSpec[] globalSpecs, Span<byte> globalSpecified, GeneratedArgumentSpec[] actionSpecs, Span<byte> actionSpecified, ReadOnlySpan<char> key)
        {
            ValidateRequired(globalSpecs, globalSpecified);
            ValidateRequired(actionSpecs, actionSpecified);
            throw new UnexpectedArgException("Unexpected named argument: " + key.ToString());
        }

        private static int FindNamedArgument(GeneratedArgumentSpec[] specs, ReadOnlySpan<char> key, string token, bool hasInlineValue)
        {
            var index = FindArgument(specs, key);
            if (hasInlineValue && token[0] == '-')
            {
                // Classic collections require an explicit long-form alias (e.g. ArgShortcut("--values")).
                // Keep the existing direct scalar shorthand while preserving the collection contract.
                var longFormIndex = FindArgument(specs, token.AsSpan(1, token.IndexOf('=') - 1));
                if (longFormIndex >= 0) return longFormIndex;
                if (index >= 0 && specs[index].CollectionReviver != null) return -1;
            }
            return index;
        }

        private static int FindArgument(GeneratedArgumentSpec[] specs, ReadOnlySpan<char> key)
        {
            for (var argumentIndex = 0; argumentIndex < specs.Length; argumentIndex++)
            {
                var aliases = specs[argumentIndex].Aliases;
                for (var aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
                {
                    var comparison = specs[argumentIndex].IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                    if (key.Equals(aliases[aliasIndex].AsSpan(), comparison)) return argumentIndex;
                }
            }
            return -1;
        }

        private static int FindPositionedArgument(GeneratedArgumentSpec[] specs, int position)
        {
            for (var i = 0; i < specs.Length; i++) if (specs[i].Position == position) return i;
            return -1;
        }

        private static void PopulateArgument(GeneratedArgumentModel model, CommandLineArgumentsDefinition definition, object instance, GeneratedArgumentSpec spec, int argumentIndex, string argumentValue, Span<byte> specified, ReadOnlySpan<string> additionalValues = default)
        {
            var argument = definition.Arguments[argumentIndex];
            var revived = ReviveArgument(argument, spec, ref argumentValue, additionalValues);
            specified[argumentIndex] = 1;
            argument.RevivedValue = revived;
            model.SetValue(instance, argumentIndex, revived);
        }

        private static object ReviveArgument(CommandLineArgument argument, GeneratedArgumentSpec spec, ref string argumentValue, ReadOnlySpan<string> additionalValues = default)
        {
            var context = ArgHook.HookContext.Current;
            if (spec.RequiresHookProcessing && context != null)
            {
                var previousArgument = context.CurrentArgument;
                var previousValue = context.ArgumentValue;
                var previousRevived = context.RevivedProperty;
                try
                {
                    context.CurrentArgument = argument;
                    context.ArgumentValue = argumentValue;
                    argument.RunBeforePopulateProperty(context);
                    argumentValue = context.ArgumentValue;
                    if (spec.RequiresValidation) argument.Validate(ref argumentValue);
                    var revived = Revive(spec, argumentValue, additionalValues);
                    argument.RevivedValue = revived;
                    context.RevivedProperty = revived;
                    argument.RunAfterPopulateProperty(context);
                    return revived;
                }
                finally
                {
                    context.CurrentArgument = previousArgument;
                    context.ArgumentValue = previousValue;
                    context.RevivedProperty = previousRevived;
                }
            }

            if (spec.RequiresValidation) argument.Validate(ref argumentValue);
            return Revive(spec, argumentValue, additionalValues);
        }

        private static bool IsExplicitBooleanValue(string value) =>
            value == "0" || value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase);

        private static bool RequiresHookProcessing(GeneratedArgumentSpec[] specs)
        {
            for (var i = 0; i < specs.Length; i++) if (specs[i].RequiresHookProcessing) return true;
            return false;
        }

        private static object Revive(GeneratedArgumentSpec spec, string argumentValue, ReadOnlySpan<string> additionalValues)
        {
            try
            {
                if (spec.CollectionReviver != null) return spec.CollectionReviver(spec.Aliases.Length > 0 ? spec.Aliases[0] : spec.Name, argumentValue, additionalValues);
                // User overrides must win even when a built-in TryParse would succeed.
                // Enum revival follows its separate classic alias/case contract.
                if (spec.ArgumentType.IsEnum == false && ArgRevivers.HasCustomReviver(spec.ArgumentType)) return ArgRevivers.Revive(spec.ArgumentType, spec.Aliases.Length > 0 ? spec.Aliases[0] : spec.Name, argumentValue);
                if (spec.ArgumentType == typeof(string)) return argumentValue;
                if (spec.ArgumentType == typeof(bool)) return argumentValue.Equals("false", StringComparison.OrdinalIgnoreCase) == false && argumentValue != "0";
                if (spec.ArgumentType == typeof(int) && int.TryParse(argumentValue, out var intValue)) return intValue;
                if (spec.ArgumentType == typeof(long) && long.TryParse(argumentValue, out var longValue)) return longValue;
                if (spec.ArgumentType == typeof(byte) && byte.TryParse(argumentValue, out var byteValue)) return byteValue;
                if (spec.ArgumentType == typeof(float) && float.TryParse(argumentValue, out var floatValue)) return floatValue;
                if (spec.ArgumentType == typeof(double) && double.TryParse(argumentValue, out var doubleValue)) return doubleValue;
                if (spec.ArgumentType == typeof(Guid) && Guid.TryParse(argumentValue, out var guidValue)) return guidValue;
                if (spec.ArgumentType == typeof(DateTime) && DateTime.TryParse(argumentValue, out var dateTimeValue)) return dateTimeValue;
                if (spec.ArgumentType.IsEnum) return ArgRevivers.ReviveEnum(spec.ArgumentType, argumentValue, spec.IgnoreCase);
                return ArgRevivers.Revive(spec.ArgumentType, spec.Aliases.Length > 0 ? spec.Aliases[0] : spec.Name, argumentValue);
            }
            catch (ArgException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ex.InnerException is ArgException argException) throw argException;
                throw new ArgException(ex.Message, ex);
            }
        }
    }
}
