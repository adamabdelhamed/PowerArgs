using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PowerArgs
{
    /// <summary>The statically selected parsing implementation for an argument scaffold.</summary>
    public enum ArgumentPipelineKind
    {
        Reflection,
        GeneratedSharedParser,
        GeneratedDirectParser,
    }

    /// <summary>
    /// Holds source-generated argument model implementations. Applications normally do not call this API directly.
    /// </summary>
    public static class GeneratedArgumentModelRegistry
    {
        private static readonly ConcurrentDictionary<Type, GeneratedArgumentModel> Models = new ConcurrentDictionary<Type, GeneratedArgumentModel>();

        /// <summary>
        /// Registers a source-generated argument model implementation.
        /// </summary>
        public static void Register<T>(Func<T> factory, Action<CommandLineArgumentsDefinition> configureDefinition, Action<T, int, object> setValue) where T : class
        {
            Register(factory, configureDefinition, setValue, null);
        }

        /// <summary>
        /// Registers a source-generated argument model implementation with an optional direct parsing schema.
        /// </summary>
        public static void Register<T>(Func<T> factory, Action<CommandLineArgumentsDefinition> configureDefinition, Action<T, int, object> setValue, GeneratedArgumentSpec[]? directArguments) where T : class
        {
            Register(factory, configureDefinition, setValue, directArguments, null, null, null);
        }

        /// <summary>
        /// Registers a generated parsing and invocation implementation.
        /// </summary>
        public static void Register<T>(Func<T> factory, Action<CommandLineArgumentsDefinition> configureDefinition, Action<T, int, object> setValue, GeneratedArgumentSpec[]? directArguments, Action<T>? mainInvoker, Func<T, Task>? asyncMainInvoker, GeneratedActionSpec[]? actions) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (configureDefinition == null) throw new ArgumentNullException(nameof(configureDefinition));
            if (setValue == null) throw new ArgumentNullException(nameof(setValue));

            Models[typeof(T)] = new GeneratedArgumentModel(() => factory(), configureDefinition, (instance, index, value) => setValue((T)instance, index, value), directArguments, mainInvoker == null ? null : instance => mainInvoker((T)instance), asyncMainInvoker == null ? null : instance => asyncMainInvoker((T)instance), actions);
        }

        /// <summary>
        /// Returns true when the given type has a source-generated parsing implementation.
        /// </summary>
        public static bool IsGenerated(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Models.ContainsKey(type);
        }

        /// <summary>
        /// Returns true when the given type has a source-generated parsing implementation.
        /// </summary>
        public static bool IsGenerated<T>() where T : class => IsGenerated(typeof(T));

        /// <summary>Reports the selected tier without constructing or parsing the scaffold.</summary>
        public static ArgumentPipelineKind GetPipelineKind(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var model = Find(type);
            if (model == null) return ArgumentPipelineKind.Reflection;
            return model.DirectArguments == null ? ArgumentPipelineKind.GeneratedSharedParser : ArgumentPipelineKind.GeneratedDirectParser;
        }

        /// <summary>Reports the selected tier without constructing or parsing the scaffold.</summary>
        public static ArgumentPipelineKind GetPipelineKind<T>() where T : class => GetPipelineKind(typeof(T));

        internal static GeneratedArgumentModel? Find(Type type)
        {
            Models.TryGetValue(type, out var model);
            return model;
        }
    }

    internal sealed class GeneratedArgumentModel
    {
        internal Func<object> Factory { get; }
        internal Action<CommandLineArgumentsDefinition> ConfigureDefinition { get; }
        internal Action<object, int, object> SetValue { get; }
        internal GeneratedArgumentSpec[]? DirectArguments { get; }
        internal Action<object>? MainInvoker { get; }
        internal Func<object, Task>? AsyncMainInvoker { get; }
        internal GeneratedActionSpec[]? Actions { get; }

        internal GeneratedArgumentModel(Func<object> factory, Action<CommandLineArgumentsDefinition> configureDefinition, Action<object, int, object> setValue, GeneratedArgumentSpec[]? directArguments, Action<object>? mainInvoker, Func<object, Task>? asyncMainInvoker, GeneratedActionSpec[]? actions)
        {
            Factory = factory;
            ConfigureDefinition = configureDefinition;
            SetValue = setValue;
            DirectArguments = directArguments;
            MainInvoker = mainInvoker;
            AsyncMainInvoker = asyncMainInvoker;
            Actions = actions;
        }
    }

    /// <summary>
    /// Describes a source-generated command action and its direct invocation delegate.
    /// </summary>
    public sealed class GeneratedActionSpec
    {
        public string Name { get; }
        public bool IgnoreCase { get; }
        public string[] Aliases { get; }
        internal GeneratedArgumentSpec[] Arguments { get; }
        internal Func<object>? ArgumentFactory { get; }
        internal Action<object, int, object>? SetArgument { get; }
        internal bool UsesArgumentModel { get; }
        private Action<object, object?>? Invoker { get; }
        private Func<object, object?, Task>? AsyncInvoker { get; }

        public GeneratedActionSpec(string name, bool ignoreCase, Action<object> invoker, params string[] aliases)
            : this(name, ignoreCase, static () => null!, null, Array.Empty<GeneratedArgumentSpec>(), false, Adapt(invoker), null, aliases)
        {
        }

        public GeneratedActionSpec(string name, bool ignoreCase, Func<object, Task> invoker, params string[] aliases)
            : this(name, ignoreCase, static () => null!, null, Array.Empty<GeneratedArgumentSpec>(), false, null, Adapt(invoker), aliases)
        {
        }

        public GeneratedActionSpec(string name, bool ignoreCase, Func<object> argumentFactory, Action<object, int, object> setArgument, GeneratedArgumentSpec[] arguments, bool usesArgumentModel, Action<object, object?> invoker, params string[] aliases)
            : this(name, ignoreCase, argumentFactory, setArgument, arguments, usesArgumentModel, invoker, null, aliases)
        {
        }

        public GeneratedActionSpec(string name, bool ignoreCase, Func<object> argumentFactory, Action<object, int, object> setArgument, GeneratedArgumentSpec[] arguments, bool usesArgumentModel, Func<object, object?, Task> invoker, params string[] aliases)
            : this(name, ignoreCase, argumentFactory, setArgument, arguments, usesArgumentModel, null, invoker, aliases)
        {
        }

        private GeneratedActionSpec(string name, bool ignoreCase, Func<object> argumentFactory, Action<object, int, object>? setArgument, GeneratedArgumentSpec[] arguments, bool usesArgumentModel, Action<object, object?>? invoker, Func<object, object?, Task>? asyncInvoker, string[] aliases)
        {
            Name = name;
            IgnoreCase = ignoreCase;
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
            ArgumentFactory = Arguments.Length == 0 ? null : argumentFactory ?? throw new ArgumentNullException(nameof(argumentFactory));
            SetArgument = Arguments.Length == 0 ? null : setArgument ?? throw new ArgumentNullException(nameof(setArgument));
            UsesArgumentModel = usesArgumentModel;
            Invoker = invoker;
            AsyncInvoker = asyncInvoker;
            if (Invoker == null && AsyncInvoker == null) throw new ArgumentNullException(nameof(invoker));
            Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        }

        private static Action<object, object?> Adapt(Action<object> invoker)
        {
            if (invoker == null) throw new ArgumentNullException(nameof(invoker));
            return (instance, _) => invoker(instance);
        }

        private static Func<object, object?, Task> Adapt(Func<object, Task> invoker)
        {
            if (invoker == null) throw new ArgumentNullException(nameof(invoker));
            return (instance, _) => invoker(instance);
        }

        internal bool IsMatch(string value)
        {
            var comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            for (var i = 0; i < Aliases.Length; i++) if (string.Equals(Aliases[i], value, comparison)) return true;
            return false;
        }

        internal void Invoke(object instance, object? arguments)
        {
            if (Invoker != null) Invoker(instance, arguments);
            else AsyncInvoker!(instance, arguments).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Describes one argument in the source-generated direct parsing path.
    /// </summary>
    public sealed class GeneratedArgumentSpec
    {
        /// <summary>Optional typed array/list converter selected by the source generator.</summary>
        public GeneratedCollectionReviver? CollectionReviver { get; init; }

        public string Name { get; }
        public Type ArgumentType { get; }
        public bool IsRequired { get; }
        public bool IsBoolean { get; }
        public int Position { get; }
        public bool IgnoreCase { get; }
        public bool RequiresValidation { get; }
        internal bool RequiresHookProcessing { get; }
        public string[] Aliases { get; }

        public GeneratedArgumentSpec(string name, Type argumentType, bool isRequired, params string[] aliases)
            : this(name, argumentType, isRequired, -1, true, false, false, aliases)
        {
        }

        public GeneratedArgumentSpec(string name, Type argumentType, bool isRequired, int position, bool ignoreCase, bool requiresValidation, params string[] aliases)
            : this(name, argumentType, isRequired, position, ignoreCase, requiresValidation, false, aliases)
        {
        }

        public GeneratedArgumentSpec(string name, Type argumentType, bool isRequired, int position, bool ignoreCase, bool requiresValidation, bool requiresHookProcessing, params string[] aliases)
        {
            Name = name;
            ArgumentType = argumentType;
            IsRequired = isRequired;
            IsBoolean = argumentType == typeof(bool);
            Position = position;
            IgnoreCase = ignoreCase;
            RequiresValidation = requiresValidation;
            RequiresHookProcessing = requiresHookProcessing;
            Aliases = aliases;
        }
    }
}
