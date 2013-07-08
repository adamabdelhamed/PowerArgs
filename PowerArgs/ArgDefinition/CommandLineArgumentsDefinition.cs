using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// This is the root class used to define a program's command line arguments.  You can start with an empty definition and 
    /// programatically add arguments or you can start from a Type that you have defined and have the definition inferred from it.
    /// </summary>
    public class CommandLineArgumentsDefinition
    {
        /// <summary>
        /// The type that was used to generate this definition.  This will only be populated if you use the constructor that takes in a type and the definition is inferred.
        /// </summary>
        public Type ArgumentScaffoldType { get; private set; }

        /// <summary>
        /// The command line arguments that are global to this definition.
        /// </summary>
        public List<CommandLineArgument> Arguments { get; private set; }

        /// <summary>
        /// Global hooks that can execute all hook override methods except those that target a particular argument.
        /// </summary>
        public List<ArgHook> Hooks { get; private set; }
        
        /// <summary>
        /// Actions that are defined for this definition.  If you have at least one action then the end user must specify the action as the first argument to your program.
        /// </summary>
        public List<CommandLineAction> Actions { get; private set; }

        /// <summary>
        /// Examples that show users how to use your program.
        /// </summary>
        public List<ArgExample> Examples { get; private set; }

        /// <summary>
        /// Determines how end user errors should be handled by the parser.  By default all exceptions flow through to your program.
        /// </summary>
        public ArgExceptionBehavior ExceptionBehavior { get; set; }

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
        }

        /// <summary>
        /// Creates an empty command line arguments definition.
        /// </summary>
        public CommandLineArgumentsDefinition()
        {
            Arguments = new List<CommandLineArgument>();
            Hooks = new List<ArgHook>();
            Actions = new List<CommandLineAction>();
            ExceptionBehavior = new ArgExceptionBehavior();
            Examples = new List<ArgExample>();
        }

        /// <summary>
        /// Creates a command line arguments definition and infers things like Arguments, Actions, etc. from the type's metadata.
        /// </summary>
        /// <param name="t">The argument scaffold type used to infer the definition</param>
        public CommandLineArgumentsDefinition (Type t)
        {
            PropertyInitializer.InitializeFields(this, 1);
            ArgumentScaffoldType = t;

            Examples.AddRange(t.Attrs<ArgExample>());
            ExceptionBehavior = t.HasAttr<ArgExceptionBehavior>() ? t.Attr<ArgExceptionBehavior>() : new ArgExceptionBehavior();
            Arguments.AddRange(FindCommandLineArguments(t));
            Actions.AddRange(FindCommandLineActions(t));
            Hooks.AddRange(t.Attrs<ArgHook>());
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
            ret += "(Hooks=" + Hooks.Count + ")";

            return ret;
        }




        internal void SetPropertyValues(object o)
        {
            foreach (var argument in Arguments)
            {
                var property = argument.Source as PropertyInfo;
                if (property == null) return;
                property.SetValue(o, argument.RevivedValue, null);
            }
        }

        internal void Validate()
        {
            ValidateArguments(Arguments);

            foreach (var action in Actions)
            {
                if (action.Aliases.Count == 0) throw new InvalidArgDefinitionException("One of your actions has no aliases");
                ValidateArguments(Arguments.Union(action.Arguments));
                if (action.ActionMethod == null) throw new InvalidArgDefinitionException("The action '"+action.DefaultAlias+"' has no ActionMethod defined");
            }
        }

        internal static List<string> FindAliases(PropertyInfo property)
        {
            List<string> ret = new List<string>();

            var name = property.Name;

            if (CommandLineAction.IsActionImplementation(property) && name.EndsWith(Constants.ActionArgConventionSuffix))
            {
                name = name.Substring(0, name.Length - Constants.ActionArgConventionSuffix.Length);
            }

            ret.Add(name);
            ret.AddRange(ArgShortcut.GetShortcutsInternal(property));

            bool removeName = property.Attrs<ArgShortcut>().Where(s => s.Policy == ArgShortcutPolicy.ShortcutsOnly).Count() > 0;

            if (removeName) ret.RemoveAt(0);
            return ret;
        }




        private static List<CommandLineAction> FindCommandLineActions(Type t)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            var actions = (from p in t.GetProperties(flags)
                           where  CommandLineAction.IsActionImplementation(p)
                           select CommandLineAction.Create(p)).ToList();

            if (t.HasAttr<ArgActionType>())
            {
                t = t.Attr<ArgActionType>().ActionType;
                flags = BindingFlags.Static | BindingFlags.Public;
            }

            foreach (var action in t.GetMethods(flags).Where(m => CommandLineAction.IsActionImplementation(m)).Select(m => CommandLineAction.Create(m)))
            {
                var matchingPropertyBasedAction = actions.Where(a => a.Aliases.First() == action.Aliases.First()).SingleOrDefault();
                if (matchingPropertyBasedAction != null) continue;
                actions.Add(action);
            }

            return actions;
        }

        private static List<CommandLineArgument> FindCommandLineArguments(Type t)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            var ret = from p in t.GetProperties(flags) 
                      where  CommandLineArgument.IsArgument(p) 
                      select CommandLineArgument.Create(p);
            return ret.ToList();
        }

        private static void ValidateArguments(IEnumerable<CommandLineArgument> arguments)
        {
            List<string> knownAliases = new List<string>();

            foreach (var argument in arguments)
            {
                foreach (var alias in argument.Aliases)
                {
                    if (knownAliases.Contains(alias)) throw new InvalidArgDefinitionException("Duplicate alias '" + alias + "' on argument '" + argument.Aliases.First() + "'");
                    knownAliases.Add(alias);
                }
            }

            foreach (var argument in arguments)
            {
                if (argument.ArgumentType == null)
                {
                    throw new InvalidArgDefinitionException("Argument '" + argument.DefaultAlias + "' has a null ArgumentType");
                }

                if (ArgRevivers.CanRevive(argument.ArgumentType) == false)
                {
                    throw new InvalidArgDefinitionException("There is no reviver for type '" + argument.ArgumentType.Name + '"');
                }

                if (argument.ArgumentType.IsEnum)
                {
                    argument.ArgumentType.ValidateNoDuplicateEnumShortcuts(argument.IgnoreCase);
                }
            }
        }
    }
}
