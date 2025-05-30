﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private AttrOverride overrides;

        /// <summary>
        /// The values that the user can specify on the command line to specify this action.
        /// </summary>
        public AliasCollection Aliases { get; private set; }

        /// <summary>
        /// The action specific arguments that are applicable to the end user should they specify this action.
        /// </summary>
        public List<CommandLineArgument> Arguments { get; private set; }

        /// <summary>
        /// Gets the list of arguments, filtering out those that have the ArgHiddenFromUsage attribute
        /// </summary>
        public List<CommandLineArgument> UsageArguments
        {
            get
            {
                var ret = Arguments.Where(a => !a.OmitFromUsage).ToList();
                return ret;
            }
        }

        /// <summary>
        /// Creates a usage summary string that is specific to this action and accounts for positional argument, etc.
        /// </summary>
        public string UsageSummary
        {
            get
            {
                return MakeUsageSummary(false);
            }
        }

        /// <summary>
        /// Creates a usage summary string that is specific to this action and accounts for positional argument, etc. where the
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

            if (htmlEncodeBrackets)
            {
                gt = "&gt;";
                lt = "&lt;";
            }

            string ret = "";

            ret += DefaultAlias + " ";

            foreach (var positionArg in (from a in UsageArguments where a.Position >= 1 select a).OrderBy(a => a.Position))
            {
                if (positionArg.IsRequired)
                {
                    ret += lt + positionArg.DefaultAlias + gt+" ";
                }
                else
                {
                    ret += "[" + lt + positionArg.DefaultAlias + gt + "] ";
                }
            }

            if (UsageArguments.Any(a => a.Position < 0))
            {
                ret += "-options";
            }

            return ret;
        }

        /// <summary>
        /// The description that will be shown in the auto generated usage.
        /// </summary>
        public string Description
        {
            get
            {
                return overrides.Get<ArgDescription, string, ICommandLineActionMetadata>("Description", Metadata, d => d.Description, string.Empty);
            }
            set
            {
                overrides.Set("Description", value);
            }
        }
        
        /// <summary>
        /// The method or property that was used to define this action.
        /// </summary>
        public object Source { get; set; }

        /// <summary>
        /// This will be set by the parser if the parse was successful and this was the action the user specified.
        /// </summary>
        public bool IsSpecifiedAction { get; internal set; }

        /// <summary>
        /// Indicates whether or not the parser should ignore case when matching a user string with this action.
        /// </summary>
        public bool IgnoreCase
        {
            get
            {
                return overrides.GetStruct<ArgIgnoreCase, bool, ICommandLineActionMetadata>("IgnoreCase", Metadata, i => i.IgnoreCase, true);
            }
            set
            {
                overrides.Set("IgnoreCase", value);
            }
        }

        /// <summary>
        /// Specifies whether this action should be omitted from usage documentation
        /// </summary>
        public bool OmitFromUsage
        {
            get => overrides.GetStruct<OmitFromUsageDocs, bool, ICommandLineActionMetadata>("OmitFromUsage", Metadata, p => true, false);
            set => overrides.Set("OmitFromUsage", value);
        }

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

        private List<ICommandLineActionMetadata> metadata;
        /// <summary>
        /// The list of metadata that can be used to inject behavior into the action
        /// </summary>
        public List<ICommandLineActionMetadata> Metadata
        {
            get
            {
                return metadata;
            }
            private set
            {
                metadata = value;
            }
        }


        /// <summary>
        /// The implementation of the action that can be invoked by the parser if the user specifies this action.
        /// </summary>
        public MethodInfo ActionMethod { get; set; }

        /// <summary>
        /// Returns true if there is at least 1 ArgExample metadata on this action
        /// </summary>
        public bool HasExamples
        {
            get
            {
                for(var i = 0; i < Metadata.Count; i++)
                {
                    if (Metadata[i] is ArgExample)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if this action has at least 1 action specific argument
        /// </summary>
        public bool HasArguments
        {
            get
            {
                return Arguments.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if there is at least 1 global argument that should be visible in usage, false otherwise
        /// </summary>
        public bool HasUsageArguments
        {
            get
            {
                return UsageArguments.Count > 0;
            }
        }

        /// <summary>
        /// Examples that show users how to use this action.
        /// </summary>
        public ReadOnlyCollection<ArgExample> Examples
        {
            get
            {
                var ret = new List<ArgExample>();
                for (var i = 0; i < Metadata.Count; i++)
                {
                    if (Metadata[i] is ArgExample)
                    {
                        ret.Add((ArgExample)Metadata[i]);
                    }
                }
                ret.Sort((a, b) => b.Example.CompareTo(a.Example));
                return ret.AsReadOnly();
            }
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

        /// <summary>
        /// Determines if 2 actions are the same based on their source.  
        /// </summary>
        /// <param name="obj">The other action</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            var other = obj as CommandLineAction;
            if (other == null) return false;

            if (this.Source == other.Source)
            {
                return true;
            }

            // TODO - improve robustness of this equals

            return false;
        }

        public CommandLineAction()
        {
            overrides = new AttrOverride(GetType());
            Aliases = new AliasCollection(() => 
            {
                var list = new List<ArgShortcut>();
                for (var i = 0; i < Metadata.Count; i++)
                {
                    if (Metadata[i] is ArgShortcut s)
                    {
                        list.Add(s);
                    }
                }
                return list;
            }, () => { return IgnoreCase; },stripLeadingArgInticatorsOnAttributeValues: false);
            PropertyInitializer.InitializeFields(this, 1);
            IgnoreCase = true;
            Metadata = new List<ICommandLineActionMetadata>();
            Arguments = new List<CommandLineArgument>();
        }

        /// <summary>
        /// Creates a new command line action given an implementation.
        /// </summary>
        /// <param name="actionHandler">The implementation of the action.</param>
        public CommandLineAction(Action<CommandLineArgumentsDefinition> actionHandler) : this()
        {
            overrides = new AttrOverride(GetType());
            PropertyInitializer.InitializeFields(this, 1);
            ActionMethod = new ActionMethodInfo(actionHandler);
            Source = ActionMethod;
            IgnoreCase = true;
        }

        /// <summary>
        /// Creates a new command line action given an implementation.
        /// </summary>
        /// <param name="actionHandler">The implementation of the action.</param>
        public CommandLineAction(Func<CommandLineArgumentsDefinition,Task> actionHandler) : this()
        {
            overrides = new AttrOverride(GetType());
            PropertyInitializer.InitializeFields(this, 1);
            ActionMethod = new FuncMethodInfo(actionHandler);
            Source = ActionMethod;
            IgnoreCase = true;
        }

        internal static CommandLineAction Create(PropertyInfo actionProperty, List<string> knownAliases)
        {
            var ret = PropertyInitializer.CreateInstance<CommandLineAction>();
            ret.ActionMethod = ArgAction.ResolveMethod(actionProperty.DeclaringType, actionProperty);
            ret.Source = actionProperty;
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

            ret.Metadata.AddRange(actionProperty.Attrs<IArgMetadata>().AssertAreAllInstanceOf<ICommandLineActionMetadata>());

            // This line only calls into CommandLineArgument because the code to strip 'Args' off the end of the
            // action property name lives here.  This is a pre 2.0 hack that's only left in place to support apps that
            // use the 'Args' suffix pattern.
            ret.Aliases.AddRange(CommandLineArgument.FindDefaultShortcuts(actionProperty, knownAliases, ret.IgnoreCase));

            return ret;
        }

        internal static CommandLineAction Create(MethodInfo actionMethod, List<string> knownAliases)
        {
            var ret = PropertyInitializer.CreateInstance<CommandLineAction>();
            ret.ActionMethod = actionMethod;

            ret.Source = actionMethod;
            if (actionMethod.Attrs<ArgShortcut>().Where(s => s.Policy == ArgShortcutPolicy.ShortcutsOnly).None())
            {
                ret.Aliases.Add(actionMethod.Name);
            }

            ret.Metadata.AddRange(actionMethod.Attrs<IArgMetadata>().AssertAreAllInstanceOf<ICommandLineActionMetadata>());

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
            else if (actionMethod.GetParameters().Length > 0)
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

            var ret = ObjectFactory.CreateInstance(actionArgsType);
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
            var ret = Aliases.Where(a => a.Equals(actionString, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).Any();
            return ret;
        }

        internal static bool IsActionImplementation(MethodInfo method)
        {
            return method.HasAttr<ArgActionMethod>();
        }

        internal static bool IsActionImplementation(PropertyInfo property)
        {
            return property.Name.EndsWith(Constants.ActionArgConventionSuffix) && 
                   property.HasAttr<ArgIgnoreAttribute>() == false &&
                ArgAction.GetActionProperty(property.DeclaringType) != null;
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
    }
}
