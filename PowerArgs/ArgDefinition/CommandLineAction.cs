using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// A class that represents command line actions that users can specify on the command line.  This is useful for programs like git
    /// where users first specify an action like 'push' and then the remaining arguments are either global or specific to 'push'.
    /// </summary>
    public class CommandLineAction
    {
        /// <summary>
        /// The implementation of the action that can be invoked by the parser if the user specifies this action.
        /// </summary>
        public MethodInfo ActionMethod { get; private set; }

        /// <summary>
        /// The values that the user can specify on the command line to specify this action.
        /// </summary>
        public List<string> Aliases { get; private set; }

        /// <summary>
        /// The action specific arguments that are applicable to the end user should they specify this action.
        /// </summary>
        public List<CommandLineArgument> Arguments { get; private set; }

        /// <summary>
        /// The description that will be shown in the auto generated usage.
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// The method or property that was used to define this action.
        /// </summary>
        public object Source { get; set; }

        /// <summary>
        /// Examples that show users how to use this action.
        /// </summary>
        public List<ArgExample> Examples { get; private set; }

        /// <summary>
        /// This will be set by the parser if the parse was successful and this was the action the user specified.
        /// </summary>
        public bool IsSpecifiedAction { get; internal set; }

        /// <summary>
        /// Indicates whether or not the parser should ignore case when matching a user string with this action.
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// The first alias or null if there are no aliases.
        /// </summary>
        public string DefaultAlias
        {
            get
            {
                return Aliases.FirstOrDefault();
            }
        }

        /// <summary>
        /// Creates a new command line action given an implementation.
        /// </summary>
        /// <param name="actionHandler">The implementation of the aciton.</param>
        public CommandLineAction(Action<CommandLineArgumentsDefinition> actionHandler) : this()
        {
            ActionMethod = new ActionMethodInfo(actionHandler);
            Source = ActionMethod;
        }

        /// <summary>
        /// Gets a string representation of this action.
        /// </summary>
        /// <returns>a string representation of this action</returns>
        public override string ToString()
        {
            var ret = "";
            if (Aliases.Count > 0) ret += DefaultAlias;
            ret += "(Aliases=" + Aliases.Count + ")";
            ret += "(Arguments=" + Arguments.Count + ")";

            return ret;
        }

        internal CommandLineAction()
        {
            PropertyInitializer.InitializeFields(this, 1);
            IgnoreCase = true;
        }

        internal static CommandLineAction Create(PropertyInfo actionProperty, List<string> knownAliases)
        {
            var ret = PropertyInitializer.CreateInstance<CommandLineAction>();
            ret.ActionMethod = ArgAction.ResolveMethod(actionProperty.DeclaringType, actionProperty);
            ret.Examples.AddRange(actionProperty.Attrs<ArgExample>());
            ret.Source = actionProperty;
            ret.Description = actionProperty.HasAttr<ArgDescription>() ? actionProperty.Attr<ArgDescription>().Description : "";
            ret.Arguments.AddRange(new CommandLineArgumentsDefinition(actionProperty.PropertyType).Arguments);
            ret.IgnoreCase = true;

            if (actionProperty.DeclaringType.HasAttr<ArgIgnoreCase>() && actionProperty.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ret.IgnoreCase = false;
            }

            if (actionProperty.HasAttr<ArgIgnoreCase>() && actionProperty.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ret.IgnoreCase = false;
            }

            ret.Aliases.AddRange(CommandLineArgument.FindAliases(actionProperty, knownAliases, ret.IgnoreCase));

            return ret;
        }

        internal static CommandLineAction Create(MethodInfo actionMethod, List<string> knownAliases)
        {
            var ret = PropertyInitializer.CreateInstance<CommandLineAction>();
            ret.ActionMethod = actionMethod;

            ret.Source = actionMethod;
            ret.Aliases.AddRange(FindAliases(actionMethod));
            ret.Description = actionMethod.HasAttr<ArgDescription>() ? actionMethod.Attr<ArgDescription>().Description : "";
            ret.Examples.AddRange(actionMethod.Attrs<ArgExample>());

            ret.IgnoreCase = true;

            if (actionMethod.DeclaringType.HasAttr<ArgIgnoreCase>() && actionMethod.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ret.IgnoreCase = false;
            }

            if (actionMethod.HasAttr<ArgIgnoreCase>() && actionMethod.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ret.IgnoreCase = false;
            }

            if (actionMethod.GetParameters().Length == 1 && ArgRevivers.CanRevive(actionMethod.GetParameters()[0].ParameterType) == false)
            {
                ret.Arguments.AddRange(actionMethod.GetParameters()[0].ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => CommandLineArgument.IsArgument(p)).Select(p => CommandLineArgument.Create(p, knownAliases)));
            }
            else if (actionMethod.GetParameters().Length > 0 && actionMethod.GetParameters().Where(p => ArgRevivers.CanRevive(p.ParameterType) == false).Count() == 0)
            {
                ret.Arguments.AddRange(actionMethod.GetParameters().Where(p => CommandLineArgument.IsArgument(p)).Select(p => CommandLineArgument.Create(p)));
                foreach (var arg in (ret.Arguments).Where(a => a.Position >= 0))
                {
                    arg.Position++; // Since position 0 is reserved for the action specifier
                }
            }
            else if(actionMethod.GetParameters().Length > 0)
            {
                throw new InvalidArgDefinitionException("Your action method contains a parameter that cannot be revived on its own.  That is only valid if the non-revivable parameter is the only parameter.  In that case, the properties of that parameter type will be used.");
            }
            return ret;
        }

        internal object PopulateArguments(object parent, ref object[] parameters)
        {
            Type actionArgsType = null;

            if (Source is PropertyInfo)
            {
                actionArgsType = (Source as PropertyInfo).PropertyType;
            }
            else if (Source is MethodInfo && (Source as MethodInfo).GetParameters().Length > 0)
            {
                if ((Source as MethodInfo).GetParameters().Length > 1 || ArgRevivers.CanRevive((Source as MethodInfo).GetParameters()[0].ParameterType))
                {
                    parameters = Arguments.Select(a => a.RevivedValue).ToArray();
                    return null;
                }
                else
                {
                    actionArgsType = (Source as MethodInfo).GetParameters()[0].ParameterType;
                }
            }
            else
            {
                return null;
            }

            var ret = Activator.CreateInstance(actionArgsType);
            foreach (var argument in Arguments)
            {
                var argumentProperty = argument.Source as PropertyInfo;
                if (argumentProperty != null)
                {
                    argumentProperty.SetValue(ret, argument.RevivedValue, null);
                }
            }

            if (Source is PropertyInfo)
            {
                (Source as PropertyInfo).SetValue(parent, ret, null);
            }

            return ret;
        }

        internal bool IsMatch(string actionString)
        {
            var ret = Aliases.Where(a => a.Equals(actionString, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).Count() > 0;
            return ret;
        }

        internal static bool IsActionImplementation(MethodInfo method)
        {
            return method.HasAttr<ArgActionMethod>();
        }

        internal static bool IsActionImplementation(PropertyInfo property)
        {
            return property.Name.EndsWith(Constants.ActionArgConventionSuffix) && ArgAction.GetActionProperty(property.DeclaringType) != null;
        }

        private static List<string> FindAliases(MethodInfo methodAction)
        {
            List<string> ret = new List<string>();
            ret.Add(methodAction.Name);
            // TODO = Add real support for aliases for action methods
            return ret;
        }
    }
}
