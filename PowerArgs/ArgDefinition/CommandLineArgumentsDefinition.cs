using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace PowerArgs
{
    /// <summary>
    /// This is the root class used to define a program's command line arguments.  You can start with an empty definition and 
    /// programatically add arguments or you can start from a Type that you have defined and have the definition inferred from it.
    /// </summary>
    public class CommandLineArgumentsDefinition
    {
        private string exeName;

        private AttrOverride overrides;

        /// <summary>
        /// Gets or sets the ExeName for this command line argument definition's program.  If not specified the entry assembly's file name
        /// is used, without the file extension.
        /// </summary>
        public string ExeName
        {
            get
            {
                if(exeName != null)
                {
                    return exeName;
                }
                else
                {
                    try
                    {
                        var assemblyName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
                        return assemblyName;
                    }
                    catch(Exception ex)
                    {
                        return "YourProgram";
                    }
                }
            }
            set
            {
                exeName = value;
            }
        }

        /// <summary>
        /// Gets the description from ArgDescriptionMetadata if it exists, or empty string if it does not.
        /// </summary>
        public string Description
        {
            get
            {
                var meta = Metadata.Meta<ArgDescription>();
                if (meta == null) return "";
                else return meta.Description;
            }
        }

        /// <summary>
        /// Gets whether or not this program has a description
        /// </summary>
        public bool HasDescription
        {
            get
            {
                return string.IsNullOrEmpty(Description) == false;
            }
        }

        /// <summary>
        /// Returns true if there is at least 1 global argument, false otherwise
        /// </summary>
        public bool HasGlobalArguments
        {
            get
            {
                return Arguments.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if there is at least 1 action, false otherwise
        /// </summary>
        public bool HasActions
        {
            get
            {
                return this.Actions.Count > 0;
            }
        }

        /// <summary>
        /// Creates a usage summary string that takes into account actions, positional argument, etc.
        /// </summary>
        public string UsageSummary
        {
            get
            {
                return MakeUsageSummary(false);
            }
        }

        /// <summary>
        /// Creates a usage summary string that takes into account actions, positional argument, etc. where the
        /// brackets are html encoded
        /// </summary>
        public string UsageSummaryHTMLEncoded
        {
            get
            {
                return MakeUsageSummary(true);
            }
        }

        private string MakeUsageSummary(bool htmlEncodeBrackets = false)
        {
            var gt = ">";
            var lt = "<";

            if(htmlEncodeBrackets)
            {
                gt = "&gt;";
                lt = "&lt;";
            }

            string ret = "";
            ret += ExeName + " ";

            int minPosition = 0;
            if (HasActions)
            {
                ret += lt + "action" + gt + " ";
                minPosition = 1;
            }


            foreach (var positionArg in (from a in Arguments where a.Position >= minPosition select a).OrderBy(a => a.Position))
            {
                if (positionArg.IsRequired)
                {
                    ret += lt + positionArg.DefaultAlias + lt+" ";
                }
                else
                {
                    ret += "[" + lt + positionArg.DefaultAlias + gt + "] ";
                }
            }

            if (Arguments.Where(a => a.Position < 0).Count() > 0)
            {
                ret += "-options";
            }

            return ret;
        }

        /// <summary>
        /// When set to true, TabCompletion is completely disabled and required fields will ignore the PromptIfMissing flag.
        /// </summary>
        public bool IsNonInteractive { get; set; }

        /// <summary>
        /// The type that was used to generate this definition.  This will only be populated if you use the constructor that takes in a type and the definition is inferred.
        /// </summary>
        public Type ArgumentScaffoldType { get; private set; }

        /// <summary>
        /// The command line arguments that are global to this definition.
        /// </summary>
        public List<CommandLineArgument> Arguments { get; private set; }

        /// <summary>
        /// Gets all global command line arguments as well as all arguments of any actions in this definition
        /// </summary>
        public ReadOnlyCollection<CommandLineArgument> AllGlobalAndActionArguments
        {
            get
            {
                List<CommandLineArgument> ret = new List<CommandLineArgument>();
                ret.AddRange(this.Arguments);

                foreach (var action in Actions)
                {
                    ret.AddRange(action.Arguments);
                }

                return ret.AsReadOnly();
            }
        }

        /// <summary>
        /// Global hooks that can execute all hook override methods except those that target a particular argument.
        /// </summary>
        public ReadOnlyCollection<ArgHook> Hooks
        {
            get
            {
                return Metadata.Metas<ArgHook>().AsReadOnly();
            }
        }
        
        /// <summary>
        /// Actions that are defined for this definition.  If you have at least one action then the end user must specify the action as the first argument to your program.
        /// </summary>
        public List<CommandLineAction> Actions { get; private set; }

        /// <summary>
        /// Arbitrary metadata that has been added to the definition
        /// </summary>
        public List<ICommandLineArgumentsDefinitionMetadata> Metadata { get; private set; }

        /// <summary>
        /// Returns true if there is at least 1 example registered for this definition
        /// </summary>
        public bool HasExamples
        {
            get
            {
                return Examples.Count > 0;
            }
        }

        /// <summary>
        /// Examples that show users how to use your program.
        /// </summary>
        public ReadOnlyCollection<ArgExample> Examples
        {
            get
            {
                return Metadata.Metas<ArgExample>().OrderByDescending(e => e.Example).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Determines how end user errors should be handled by the parser.  By default all exceptions flow through to your program.
        /// </summary>
        public ArgExceptionBehavior ExceptionBehavior
        {
            get
            {
                return overrides.Get<ArgExceptionBehavior, ArgExceptionBehavior>("ExceptionBehavior", this.Metadata, attr => attr, new ArgExceptionBehavior(ArgExceptionPolicy.DontHandleExceptions));
            }
            set
            {
                overrides.Set("ExceptionBehavior", value);
            }
        }

        /// <summary>
        /// If your definition declares actions and has been successfully parsed then this property will be populated
        /// with the action that the end user specified.
        /// </summary>
        public CommandLineAction SpecifiedAction
        {
            get
            {
                var ret = Actions.Where(a => a.IsSpecifiedAction).SingleOrDefault();
                return ret;
            }
            set
            {
                foreach (var action in Actions)
                {
                    action.IsSpecifiedAction = false;
                }

                if (value != null)
                {
                    value.IsSpecifiedAction = true;
                }
            }
        }

        /// <summary>
        /// Returns true if this definition has been processed and an action was specified
        /// </summary>
        public bool HasSpecifiedAction
        {
            get
            {
                return SpecifiedAction != null;
            }
        }

        /// <summary>
        /// Gets any named arguments that were present on the command line, but did not match any arguments defined
        /// by the definition.  This is only valid if the AllowUnexpectedArgs metadata is present.
        /// </summary>
        public Dictionary<string,string> UnexpectedExplicitArguments { get; internal set; }

        /// <summary>
        /// Gets any positional arguments that were present on the command line, but did not match any arguments defined
        /// by the definition.  This is only valid if the AllowUnexpectedArgs metadata is present.
        /// </summary>
        public Dictionary<int, string> UnexpectedImplicitArguments { get; internal set; }

        /// <summary>
        /// Creates an empty command line arguments definition.
        /// </summary>
        public CommandLineArgumentsDefinition()
        {
            PropertyInitializer.InitializeFields(this, 1);
            overrides = new AttrOverride(GetType());
        }

        /// <summary>
        /// Creates a command line arguments definition and infers things like Arguments, Actions, etc. from the type's metadata.
        /// </summary>
        /// <param name="t">The argument scaffold type used to infer the definition</param>
        public CommandLineArgumentsDefinition (Type t) : this()
        {
            ArgumentScaffoldType = t;
            Arguments.AddRange(FindCommandLineArguments(t));
            Actions.AddRange(FindCommandLineActions(t));
            Metadata.AddRange(t.Attrs<IArgMetadata>().AssertAreAllInstanceOf<ICommandLineArgumentsDefinitionMetadata>());
        }

        /// <summary>
        /// Finds the first CommandLineArgument that matches the given key.
        /// </summary>
        /// <param name="key">The key as if it was typed in on the command line.  This can also be an alias. </param>
        /// <param name="throwIfMoreThanOneMatch">If set to true then this method will throw and InvalidArgDeginitionException if more than 1 match is found</param>
        /// <returns>The first argument that matches the key.</returns>
        public CommandLineArgument FindMatchingArgument(string key, bool throwIfMoreThanOneMatch = false)
        {
            return CommandLineArgumentsDefinition.FindMatchingArgument(key, throwIfMoreThanOneMatch, this.Arguments);
        }

        /// <summary>
        /// Finds the first CommandLineAction that matches the given key
        /// </summary>
        /// <param name="key">The key as if it was typed in on the command line.  This can also be an alias. </param>
        /// <param name="throwIfMoreThanOneMatch">If set to true then this method will throw and InvalidArgDeginitionException if more than 1 match is found</param>
        /// <returns>The first action that matches the key.</returns>
        public CommandLineAction FindMatchingAction(string key, bool throwIfMoreThanOneMatch = false)
        {
            var match = from a in Actions where a.IsMatch(key) select a;
            if (match.Count() > 1 && throwIfMoreThanOneMatch)
            {
                throw new InvalidArgDefinitionException("The key '" + key + "' matches more than one action");
            }

            return match.FirstOrDefault();
        }

        /// <summary>
        /// Gives you an object that you can use to tell if a particular argument was specified on the command line.
        /// </summary>
        /// <returns>object that you can use to tell if a particular argument was specified on the command line</returns>
        public IBooleanVariableResolver CreateVariableResolver()
        {
            return new FuncBooleanVariableResolver((variableIdentifier) =>
            {
                foreach (var argument in this.Arguments)
                {
                    if (argument.IsMatch(variableIdentifier))
                    {
                        return argument.RevivedValue != null;
                    }
                }

                if (this.SpecifiedAction != null)
                {
                    foreach (var argument in this.SpecifiedAction.Arguments)
                    {

                        if (argument.IsMatch(variableIdentifier))
                        {
                            return argument.RevivedValue != null;
                        }
                    }
                }

                throw new InvalidArgDefinitionException(string.Format("'{0}' is not a valid argument alias", variableIdentifier));
            });
        }

        internal static CommandLineArgument FindMatchingArgument(string key, bool throwIfMoreThanOneMatch, IEnumerable<CommandLineArgument> searchSpace)
        {
            var match = from a in searchSpace where a.IsMatch(key) select a;
            if (match.Count() > 1 && throwIfMoreThanOneMatch)
            {
                throw new InvalidArgDefinitionException("The key '" + key + "' matches more than one argument");
            }

            return match.FirstOrDefault();
        }

        /// <summary>
        /// Gets a basic string representation of the definition.
        /// </summary>
        /// <returns>a basic string representation of the definition</returns>
        public override string ToString()
        {
            var ret = "";

            if (ArgumentScaffoldType != null) ret += ArgumentScaffoldType.Name;
            ret += "(Arguments=" + Arguments.Count + ")";
            ret += "(Actions=" + Actions.Count + ")";
            ret += "(Hooks=" + Hooks.Count() + ")";

            return ret;
        }

        /// <summary>
        /// Resets all portions of the model that may have side effects from being run through the 
        /// argument processor.  
        /// </summary>
        public void Clean()
        {
            SpecifiedAction = null;
            foreach(var arg in AllGlobalAndActionArguments)
            {
                arg.RevivedValue = null;
                arg.RevivedValueOverride = null;
            }
        }

        internal void SetPropertyValues(object o)
        {
            foreach (var argument in Arguments)
            {
                var property = argument.Source as PropertyInfo;
                if (property == null || argument.RevivedValue == null) continue;
                property.SetValue(o, argument.RevivedValue, null);
            }
        }

        internal void Validate(ArgHook.HookContext context)
        {
            context.RunBeforeValidateDefinition();
            ValidateArguments(Arguments);
            ValidateActionAliases();
            foreach (var action in Actions)
            {
                if (action.Aliases.Count == 0) throw new InvalidArgDefinitionException("One of your actions has no aliases");
                ValidateArguments(Arguments.Union(action.Arguments));
                if (action.ActionMethod == null) throw new InvalidArgDefinitionException("The action '"+action.DefaultAlias+"' has no ActionMethod defined");
            }
        }

        private List<CommandLineAction> FindCommandLineActions(Type t)
        {
            var knownAliases = new List<string>();
            foreach (var argument in Arguments) knownAliases.AddRange(argument.Aliases);

            BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;

            var actions = (from p in t.GetProperties(flags)
                           where  CommandLineAction.IsActionImplementation(p)
                           select CommandLineAction.Create(p, knownAliases)).ToList();

            List<Type> typesToSearchForActions = new List<Type>() { t };

            if(t.HasAttr<ArgActionResolver>())
            {
                typesToSearchForActions.AddRange(t.Attr<ArgActionResolver>().ResolveActionTypes());
            }

            typesToSearchForActions.AddRange(t.Attrs<ArgActionType>().Select(aat => aat.ActionType));

            foreach (var typeToSearch in typesToSearchForActions)
            {
                var requireStatic = typeToSearch != t;
                foreach (var method in typeToSearch.GetMethods(flags).Where(m => CommandLineAction.IsActionImplementation(m)))
                {
                    if(requireStatic && method.IsStatic == false)
                    {
                        throw new InvalidArgDefinitionException("The method "+method.DeclaringType.FullName+"."+method.Name+" must be static because it has been imported using [ArgActionType] or [ArgActions]");
                    }

                    var action = CommandLineAction.Create(method, knownAliases.ToList());
                    var matchingPropertyBasedAction = actions.Where(a => a.Aliases.First() == action.Aliases.First()).SingleOrDefault();
                    if (matchingPropertyBasedAction != null) continue;
                    actions.Add(action);
                }
            }

            return actions;
        }


        private static List<CommandLineArgument> FindCommandLineArguments(Type t)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            var knownAliases = new List<string>();

            foreach (var prop in t.GetProperties(flags))
            {
                // This makes sure that explicit aliases get put into the known aliases before any auto generated aliases
                knownAliases.AddRange(prop.Attrs<ArgShortcut>().Select(s => s.Shortcut));
            }

            var ret = from p in t.GetProperties(flags) 
                      where  CommandLineArgument.IsArgument(p) 
                      select CommandLineArgument.Create(p, knownAliases);
            return ret.ToList();
        }

        private void ValidateActionAliases()
        {
            Func<List<ArgShortcut>> shortcutEval = () =>
            {
                return new List<ArgShortcut>();
            };

            AliasCollection aliases = new AliasCollection(shortcutEval, () =>
            {
                if (Actions.Count() == 0) return true;
                return Actions.First().IgnoreCase;
            });

            foreach(var action in Actions)
            {
                aliases.AddRange(action.Aliases);
            }
        }

        private static void ValidateArguments(IEnumerable<CommandLineArgument> arguments)
        {
            List<string> knownAliases = new List<string>();

            foreach (var argument in arguments)
            {
                foreach (var alias in argument.Aliases)
                {
                    if (knownAliases.Contains(alias, new CaseAwareStringComparer(argument.IgnoreCase))) throw new InvalidArgDefinitionException("Duplicate alias '" + alias + "' on argument '" + argument.Aliases.First() + "'");
                    knownAliases.Add(alias);
                }
            }

            foreach (var argument in arguments)
            {
                if (argument.ArgumentType == null)
                {
                    throw new InvalidArgDefinitionException("Argument '" + argument.DefaultAlias + "' has a null ArgumentType");
                }

                if (argument.MustBeRevivable && ArgRevivers.CanRevive(argument.ArgumentType) == false)
                {
                    throw new InvalidArgDefinitionException("There is no reviver for type '" + argument.ArgumentType.Name + '"');
                }

                if (argument.ArgumentType.IsEnum)
                {
                    argument.ArgumentType.ValidateNoDuplicateEnumShortcuts(argument.IgnoreCase);
                }


                foreach (var property in argument.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Getting each property will result in all AttrOverrides being validated
                    try
                    {
                        var val = property.GetValue(argument, null);
                    }
                    catch(TargetInvocationException ex)
                    {
                        if (ex.InnerException is InvalidArgDefinitionException)
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

            }
        }
    }
}
