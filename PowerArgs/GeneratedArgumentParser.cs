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

                    PopulateArgument(model, definition, instance, specs[positionalArgumentIndex], positionalArgumentIndex, token, specified);
                    continue;
                }

                foundNamedArgument = true;

                ReadNamedToken(token, out var key, out var argumentValue, out var hasInlineValue);

                var argumentIndex = FindArgument(specs, key);
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

                PopulateArgument(model, definition, instance, spec, argumentIndex, argumentValue, specified);
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
                    if (positionedGlobalIndex >= 0) PopulateArgument(model, definition, parent, globalSpecs[positionedGlobalIndex], positionedGlobalIndex, token, globalSpecified);
                    else PopulateActionArgument(action, actionDefinition, actionValues!, actionSpecs[positionedActionIndex], positionedActionIndex, token, actionSpecified);
                    continue;
                }

                foundNamedArgument = true;
                ReadNamedToken(token, out var key, out var argumentValue, out var hasInlineValue);

                var globalIndex = FindArgument(globalSpecs, key);
                var actionIndex = FindArgument(actionSpecs, key);
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

                if (globalIndex >= 0) PopulateArgument(model, definition, parent, spec, globalIndex, argumentValue, globalSpecified);
                else PopulateActionArgument(action, actionDefinition, actionValues!, spec, actionIndex, argumentValue, actionSpecified);
            }

            ApplyDefaults(model, definition, parent, globalSpecs, globalSpecified);
            ApplyActionDefaults(action, actionDefinition, actionValues, actionSpecs, actionSpecified);
            ValidateRequired(globalSpecs, globalSpecified);
            ValidateRequired(actionSpecs, actionSpecified);
            value = parent;
            actionArguments = actionValues;
            return true;
        }

        private static void PopulateActionArgument(GeneratedActionSpec action, CommandLineAction definition, object instance, GeneratedArgumentSpec spec, int argumentIndex, string argumentValue, Span<byte> specified)
        {
            if (spec.RequiresValidation) definition.Arguments[argumentIndex].Validate(ref argumentValue);
            var revived = Revive(spec, argumentValue);
            specified[argumentIndex] = 1;
            definition.Arguments[argumentIndex].RevivedValue = revived;
            action.SetArgument!(instance, argumentIndex, revived);
        }

        private static void ApplyDefaults(GeneratedArgumentModel model, CommandLineArgumentsDefinition definition, object instance, GeneratedArgumentSpec[] specs, Span<byte> specified)
        {
            for (var i = 0; i < specs.Length; i++)
            {
                if (specified[i] != 0 || definition.Arguments[i].HasDefaultValue == false) continue;
                PopulateArgument(model, definition, instance, specs[i], i, definition.Arguments[i].DefaultValue.ToString()!, specified);
            }
        }

        private static void ApplyActionDefaults(GeneratedActionSpec action, CommandLineAction definition, object? instance, GeneratedArgumentSpec[] specs, Span<byte> specified)
        {
            if (instance == null) return;
            for (var i = 0; i < specs.Length; i++)
            {
                if (specified[i] != 0 || definition.Arguments[i].HasDefaultValue == false) continue;
                PopulateActionArgument(action, definition, instance, specs[i], i, definition.Arguments[i].DefaultValue.ToString()!, specified);
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

        private static void PopulateArgument(GeneratedArgumentModel model, CommandLineArgumentsDefinition definition, object instance, GeneratedArgumentSpec spec, int argumentIndex, string argumentValue, Span<byte> specified)
        {
            if (spec.RequiresValidation) definition.Arguments[argumentIndex].Validate(ref argumentValue);
            var revived = Revive(spec, argumentValue);
            specified[argumentIndex] = 1;
            definition.Arguments[argumentIndex].RevivedValue = revived;
            model.SetValue(instance, argumentIndex, revived);
        }

        private static bool IsExplicitBooleanValue(string value) =>
            value == "0" || value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase);

        private static object Revive(GeneratedArgumentSpec spec, string argumentValue)
        {
            try
            {
                if (spec.ArgumentType.IsEnum) return ArgRevivers.ReviveEnum(spec.ArgumentType, argumentValue, spec.IgnoreCase);
                return ArgRevivers.Revive(spec.ArgumentType, spec.Name, argumentValue);
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
