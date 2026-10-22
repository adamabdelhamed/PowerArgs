namespace PowerArgs;

// Prepared per definition, never shared across parser threads. Keep the classic attribute
// implementations for scalar validation: their string rewriting and error contracts matter.
internal sealed class GeneratedArgumentRules
{
    private readonly ICommandLineArgumentMetadata[] metadata;
    private readonly ArgValidator[] validators;
    private readonly int[] priorities;
    private readonly ArgCantBeCombinedWith[] exclusions;
    private readonly ArgRequired? required;
    private IBooleanExpression? requiredExpression;
    private string? requiredText;
    private bool negateRequired;

    private GeneratedArgumentRules(CommandLineArgument argument)
    {
        metadata = argument.Metadata.ToArray();
        validators = metadata.OfType<ArgValidator>().OrderBy(v => v.Priority).ToArray();
        priorities = validators.Select(v => v.Priority).ToArray();
        exclusions = metadata.OfType<ArgCantBeCombinedWith>().ToArray();
        required = validators.FirstOrDefault(v => v.GetType() == typeof(ArgRequired)) as ArgRequired;
    }

    internal static GeneratedArgumentRules For(CommandLineArgument argument)
    {
        var cached = argument.GeneratedRules;
        if (cached == null || !cached.Matches(argument)) argument.GeneratedRules = cached = new GeneratedArgumentRules(argument);
        return cached;
    }

    private bool Matches(CommandLineArgument argument)
    {
        if (metadata.Length != argument.Metadata.Count) return false;
        for (var i = 0; i < metadata.Length; i++) if (!ReferenceEquals(metadata[i], argument.Metadata[i])) return false;
        for (var i = 0; i < validators.Length; i++) if (validators[i].Priority != priorities[i]) return false;
        return true;
    }

    internal void Validate(CommandLineArgument argument, ref string value)
    {
        foreach (var validator in validators)
        {
            // The direct lifecycle evaluates these after population without installing a hook.
            if (ReferenceEquals(validator, required) && required!.IsConditionallyRequired) continue;
            if (validator.ImplementsValidateAlways) validator.ValidateAlways(argument, ref value);
            else if (value != null) validator.Validate(argument.DefaultAlias, ref value);
        }
    }

    internal void AfterPopulate(CommandLineArgument argument, IBooleanVariableResolver resolver, bool validationEnabled)
    {
        // Classic hook dispatch visits arguments in definition order. Conditional required
        // hooks are appended during validation, after the argument's existing exclusion hooks.
        foreach (var exclusion in exclusions)
        {
            bool disallowed;
            try { disallowed = exclusion.GeneratedExpression.Evaluate(resolver); }
            catch (Exception ex) { throw new InvalidArgDefinitionException("Failed to evaluate conditional ArgCantBeCombinedWith clause on target '" + argument.DefaultAlias + "' - " + ex.Message); }
            if (disallowed && argument.RevivedValue != null) throw new UnexpectedArgException("The argument '" + argument.DefaultAlias + "' cannot be used with one or more arguments: " + exclusion.ExpressionText);
        }

        if (!validationEnabled || required == null || !required.IsConditionallyRequired) return;
        if (required.If != null && required.IfNot != null) throw new InvalidArgDefinitionException("You cannot specify both the 'If' and the 'IfNot' properties on the ArgRequired metadata");
        var text = required.If ?? required.IfNot!;
        var negate = required.If == null;
        try
        {
            // Parse lazily: malformed conditions must not preempt conversion/validation errors.
            if (requiredExpression == null || text != requiredText || negate != negateRequired)
            {
                requiredExpression = BooleanExpressionParser.Parse(negate ? "!(" + text + ")" : text);
                requiredText = text;
                negateRequired = negate;
            }
            if (requiredExpression.Evaluate(resolver) && argument.RevivedValue == null)
                throw new MissingArgException("The argument '" + argument.DefaultAlias + "' is required if the following argument(s) are " + (negate ? "not specified: " : "specified: ") + text);
        }
        catch (MissingArgException) { throw; }
        catch (Exception ex) { throw new InvalidArgDefinitionException("Failed to evaluate conditional ArgRequired clause on target '" + argument.DefaultAlias + " (" + text + ")'" + ex.Message); }
    }

    internal static void BeforePopulate(CommandLineArgument argument, ArgHook.HookContext context)
    {
        foreach (var item in argument.Metadata) if (item is HelpHook help) help.BeforePopulateProperty(context);
    }

    internal static void PopulateAbsentHelp(CommandLineArgument argument)
    {
        var context = ArgHook.HookContext.Current;
        var previousArgument = context.CurrentArgument;
        var previousValue = context.ArgumentValue;
        var previousRevived = context.RevivedProperty;
        try
        {
            context.CurrentArgument = argument;
            context.ArgumentValue = null;
            context.RevivedProperty = null;
            BeforePopulate(argument, context);
            AfterPopulateValue(argument, context);
        }
        finally
        {
            context.CurrentArgument = previousArgument;
            context.ArgumentValue = previousValue;
            context.RevivedProperty = previousRevived;
        }
    }

    internal static void AfterPopulateValue(CommandLineArgument argument, ArgHook.HookContext context)
    {
        foreach (var item in argument.Metadata) if (item is HelpHook help) help.AfterPopulateProperty(context);
    }
}

// Reuse the boolean language and its short-circuit rules, but resolve against live generated
// argument slots without allocating a closure/resolver for every rule on every parse.
internal sealed class GeneratedArgumentVariableResolver(CommandLineArgumentsDefinition definition) : IBooleanVariableResolver
{
    public bool ResolveBoolean(string variableIdentifier)
    {
        foreach (var argument in definition.Arguments) if (argument.IsMatch(variableIdentifier)) return argument.RevivedValue != null;
        if (definition.SpecifiedAction != null)
            foreach (var argument in definition.SpecifiedAction.Arguments) if (argument.IsMatch(variableIdentifier)) return argument.RevivedValue != null;
        throw new InvalidArgDefinitionException("'" + variableIdentifier + "' is not a valid argument alias");
    }
}
