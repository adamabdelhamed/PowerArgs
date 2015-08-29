using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace PowerArgs
{
    /// <summary>
    /// Instances of this class represent a single command line argument that users can specify on the command line.
    /// Supported syntaxes include:
    ///     -argumentName argumentValue
    ///     /argumentName:argumentValue
    ///     -argumentName                   - If the argument is a boolean it will be true in this case.
    ///     --argumentName=argumentValue    - Only works if you have added an alias that starts with --.
    ///     argumentValue                   - Only works if this argument defines the Position property as >= 0
    /// </summary>
    public class CommandLineArgument
    {
        private static Dictionary<string, string> KnownTypeMappings = new Dictionary<string, string>()
        {
            {"Int32", "integer"},
            {"Int64", "integer"},
            {"Boolean", "switch"},
            {"Guid", "guid"},
        };

        private AttrOverride overrides;

        /// <summary>
        /// The values that can be used as specifiers for this argument on the command line
        /// </summary>
        public AliasCollection Aliases { get; private set; }

        /// <summary>
        /// Metadata that has been injected into this Argument
        /// </summary>
        public List<ICommandLineArgumentMetadata> Metadata { get; private set; }

        /// <summary>
        /// Gets a friendly type name for this argument.
        /// </summary>
        public string FriendlyTypeName
        {
            get
            {
                string ret;
                if (ArgumentType.IsGenericType)
                {
                    if (ArgumentType.GetGenericArguments().Length == 1 && ArgumentType.GetGenericTypeDefinition() == typeof(Nullable<int>).GetGenericTypeDefinition())
                    {
                        ret = MapTypeName(ArgumentType.GetGenericArguments()[0]);
                    }
                    else
                    {
                        string name = GetTypeNameWithGenericsStripped(ArgumentType.GetGenericTypeDefinition());
                        string parameters = string.Join(", ", ArgumentType.GetGenericArguments().Select(a => MapTypeName(a)));
                        ret = name + "<" + parameters + ">";
                    }
                }
                else
                {
                    ret = MapTypeName(ArgumentType);
                }

                return ret;
            }
        }

        internal string PrimaryShortcutAlias
        {
            get
            {
                var aliases = Aliases.OrderBy(a => a.Length).ToList();
                var maxInlineAliasLength = 8;
                string inlineAliasInfo = "";

                int aliasIndex;
                for (aliasIndex = 0; aliasIndex < aliases.Count; aliasIndex++)
                {
                    if (aliases[aliasIndex] == DefaultAlias) continue;
                    var proposedInlineAliases = inlineAliasInfo == string.Empty ? "-"+aliases[aliasIndex] : inlineAliasInfo + ", -" + aliases[aliasIndex];
                    if (proposedInlineAliases.Length <= maxInlineAliasLength)
                    {
                        inlineAliasInfo = proposedInlineAliases;
                    }
                    else
                    {
                        break;
                    }
                }

                return inlineAliasInfo;
            }
        }

     
        internal string Syntax
        {
            get
            {
                var ret = DefaultAlias;

                if (IsRequired && Metadata.Meta<ArgRequired>().IsConditionallyRequired == false)
                {
                    ret += "*";
                }

                if(PrimaryShortcutAlias.Length > 0)
                {
                    ret += " ("+PrimaryShortcutAlias+")";
                }

                return ret;
            }
        }

        internal ReadOnlyCollection<ArgValidator> Validators
        {
            get
            {
                return Metadata.Metas<ArgValidator>().OrderByDescending(v => v.Priority).ToList().AsReadOnly();
            }
        }

        internal ReadOnlyCollection<ArgHook> Hooks
        {
            get
            {
                return Metadata.Metas<ArgHook>().AsReadOnly();
            }
        }

        internal ReadOnlyCollection<UsageHook> UsageHooks
        {
            get
            {
                return Metadata.Metas<UsageHook>().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating that this argument must be revivable from a string.  If false, the argument can only be populated
        /// programatically and if specified on the command line without a reviver the program will throw an InvalidArgDefinitionException.
        /// </summary>
        public bool MustBeRevivable { get; set; }

        /// <summary>
        /// The CLR type of this argument.
        /// </summary>
        public Type ArgumentType { get; private set; }

        /// <summary>
        /// Specifies whether or not the parser should ignore case when trying to find a match for this argument on the command line.  Defaults to true.
        /// </summary>
        public bool IgnoreCase
        {
            get
            {
                return overrides.Get<ArgIgnoreCase, bool>("IgnoreCase", Metadata, p => p.IgnoreCase, true);
            }
            set
            {
                overrides.Set("IgnoreCase", value);
            }
        }

        /// <summary>
        /// Returns true if the argument is an enum or a nullable where the value type is an enum
        /// </summary>
        public bool IsEnum
        {
            get
            {
                if (ArgumentType.IsEnum)
                {
                    return true;
                }

                if(ArgumentType.IsGenericType == false)
                {
                    return false;
                }

                if (ArgumentType.GetGenericTypeDefinition() != typeof(Nullable<>))
                {
                    return false;
                }

                var nullableType = ArgumentType.GetGenericArguments().FirstOrDefault();
                
                if(nullableType == null)
                {
                    return false;
                }

                return nullableType.IsEnum;
            }
        }

        /// <summary>
        /// Specifies whether this argument should be omitted from usage documentation
        /// </summary>
        public bool OmitFromUsage
        {
            get
            {
                return overrides.Get<OmitFromUsageDocs, bool>("OmitFromUsage", Metadata, p =>true, false);
            }
            set
            {
                overrides.Set("OmitFromUsage", value);
            }
        }

        /// <summary>
        /// True if this argument should be included in usage documentation
        /// </summary>
        public bool IncludeInUsage
        {
            get
            {
                return !OmitFromUsage;
            }
            set
            {
                OmitFromUsage = !value;
            }
        }

        /// <summary>
        /// If this is a positional argument then set this value >= 0 and users can specify a value without specifying an argument alias.  Defaults to -1.
        /// </summary>
        public int Position
        {
            get
            {
                return overrides.Get<ArgPosition, int>("Position", Metadata, p => p.Position, -1);
            }
            set
            {
                overrides.Set("Position", value);
            }
        }

        /// <summary>
        /// Returns true if a default value has been explicitly registered for this argument
        /// </summary>
        public bool HasDefaultValue
        {
            get
            {
                return DefaultValue != null;
            }
        }

        /// <summary>
        /// The default value for this argument in the event it is optional and the user did not specify it.
        /// </summary>
        public object DefaultValue
        {
            get
            {
                return overrides.Get<DefaultValueAttribute, object>("DefaultValue", Hooks, d => d.Value);
            }
            set
            {
                overrides.Set("DefaultValue", value);
            }
        }

        /// <summary>
        /// Only works if the ArgumentType is an enum or a nullable where the value type is an enum.  Returns a list where each element is a string containing an
        /// enum value and optionally its description.  Each enum value is represented in the list.
        /// </summary>
        public List<string> EnumValuesAndDescriptions
        {
            get
            {
                if (IsEnum == false)
                {
                    return new List<string>();
                }

                var enumType = ArgumentType;
                
                if(enumType.IsEnum == false)
                {
                    // we must be dealing with a nullable<enum> since IsEnum returned true above
                    enumType = enumType.GetGenericArguments()[0];
                }


                List<string> ret = new List<string>();
                foreach (var val in enumType.GetFields().Where(v => v.IsSpecialName == false))
                {
                    var description = val.HasAttr<ArgDescription>() ? " - " + val.Attr<ArgDescription>().Description : "";
                    var valText = "  " + val.Name;
                    ret.Add(val.Name + description);
                }
                return ret;
            }
        }

        /// <summary>
        /// The description for this argument that appears in the auto generated usage.
        /// </summary>
        public string Description
        {
            get
            {
                return overrides.Get<ArgDescription, string>("Description", Metadata, d => d.Description, string.Empty);
            }
            set
            {
                overrides.Set("Description", value);
            }
        }

        /// <summary>
        /// Gets or sets whether or not this argument is required.
        /// </summary>
        public bool IsRequired
        {
            get
            {
                return overrides.Get<ArgRequired, bool>("IsRequired", Validators, v => true, false);
            }
            set
            {
                overrides.Set("IsRequired", value);
            }
        }

        /// <summary>
        /// If this argument was inferred from a type then the source is either a PropertyInfo or a ParameterInfo.  If this argument
        /// was created manually then this value will be null.
        /// </summary>
        public object Source { get; set; }
        
        /// <summary>
        /// This property will contain the parsed value of the command line argument if parsing completed successfully.
        /// </summary>
        public object RevivedValue { get; set; }

        /// <summary>
        /// When set, the given value will be used to revive this argument rather than performing validation and revival.
        /// </summary>
        public object RevivedValueOverride { get; set; }

        /// <summary>
        /// The first alias of this argument or null if no aliases are defined.
        /// </summary>
        public string DefaultAlias
        {
            get
            {
                return Aliases.FirstOrDefault();
            }
        }

        internal CommandLineArgument()
        {
            MustBeRevivable = true;
            overrides = new AttrOverride(GetType());
            Aliases = new AliasCollection(() => { return Metadata.Metas<ArgShortcut>(); }, () => { return IgnoreCase; });
            PropertyInitializer.InitializeFields(this, 1);
            ArgumentType = typeof(string);
            Position = -1;
        }
 
        /// <summary>
        /// Creates a command line argument of the given type and sets the first default alias.
        /// </summary>
        /// <param name="t">The CLR type of the argument</param>
        /// <param name="defaultAlias">The default name that users will use to specify this argument</param>
        /// <param name="ignoreCase">If true, the parser will match this argument even if the specifier doesn't match case.  True by default.</param>
        public CommandLineArgument(Type t, string defaultAlias, bool ignoreCase = true) : this()
        {
            if (t == null) throw new InvalidArgDefinitionException("Argument types cannot be null");

            ArgumentType = t;
            IgnoreCase = ignoreCase;
            Aliases.Add(defaultAlias);

            Metadata.AddRange(t.Attrs<IArgMetadata>().AssertAreAllInstanceOf<ICommandLineArgumentMetadata>());
        }

        /// <summary>
        /// Tests to see if the given value would pass validation and revival.  
        /// </summary>
        /// <param name="value">the value to test</param>
        /// <returns>true if the value passes validation and is successfully revived, false otherwise</returns>
        public bool TestIsValidAndRevivable(string value)
        {
            try
            {
                var cand = value;
                Validate(ref cand);
                Revive(cand);
                RevivedValue = null;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the string representation of this argument.
        /// </summary>
        /// <returns>the string representation of this argument.</returns>
        public override string ToString()
        {
            var ret = "";
            if (Aliases.Count > 0) ret += DefaultAlias + "<" + ArgumentType.Name + ">";

            ret += "(Aliases=" + Aliases.Count + ")";
            ret += "(Validators=" + Validators.Count() + ")";
            ret += "(Hooks=" + Hooks.Count() + ")";

            return ret;
        }

        internal static CommandLineArgument Create(PropertyInfo property, List<string> knownAliases)
        {
            var ret = PropertyInitializer.CreateInstance<CommandLineArgument>();
            ret.DefaultValue = property.HasAttr<DefaultValueAttribute>() ? property.Attr<DefaultValueAttribute>().Value : null;
            ret.Position = property.HasAttr<ArgPosition>() ? property.Attr<ArgPosition>().Position : -1;
            ret.Source = property;
            ret.ArgumentType = property.PropertyType;

            ret.IgnoreCase = true;

            if (property.DeclaringType.HasAttr<ArgIgnoreCase>() && property.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ret.IgnoreCase = false;
            }

            if (property.HasAttr<ArgIgnoreCase>() && property.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ret.IgnoreCase = false;
            }


            ret.Aliases.AddRange(FindDefaultShortcuts(property, knownAliases, ret.IgnoreCase));

            // TODO - I think the first generic call can just be more specific
            ret.Metadata.AddRange(property.Attrs<IArgMetadata>().AssertAreAllInstanceOf<ICommandLineArgumentMetadata>());

            return ret;
        }

        internal static CommandLineArgument Create(ParameterInfo parameter)
        {
            var ret = PropertyInitializer.CreateInstance<CommandLineArgument>();
            ret.Position = parameter.Position;
            ret.ArgumentType = parameter.ParameterType;
            ret.Source = parameter;
            ret.DefaultValue = parameter.HasAttr<DefaultValueAttribute>() ? parameter.Attr<DefaultValueAttribute>().Value : null;
            
            ret.IgnoreCase = true;

            if (parameter.Member.DeclaringType.HasAttr<ArgIgnoreCase>() && parameter.Member.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ret.IgnoreCase = false;
            }

            if (parameter.HasAttr<ArgIgnoreCase>() && parameter.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ret.IgnoreCase = false;
            }

            ret.Aliases.Add(parameter.Name);

            ret.Metadata.AddRange(parameter.Attrs<IArgMetadata>().AssertAreAllInstanceOf<ICommandLineArgumentMetadata>());

            return ret;
        }

        internal void RunArgumentHook(ArgHook.HookContext context, Func<ArgHook, int> orderby, Action<ArgHook> hookAction)
        {
            context.Property = Source as PropertyInfo;
            context.CurrentArgument = this;

            foreach (var hook in Hooks.OrderBy(orderby))
            {
                hookAction(hook);
            }

            context.Property = null;
            context.CurrentArgument = null;
        }


        internal void RunBeforePopulateProperty(ArgHook.HookContext context)
        {
            RunArgumentHook(context, h => h.BeforePopulatePropertyPriority, (h) => { h.BeforePopulateProperty(context); });
        }

        internal void RunAfterPopulateProperty(ArgHook.HookContext context)
        {
            RunArgumentHook(context, h => h.AfterPopulatePropertyPriority, (h) => { h.AfterPopulateProperty(context); });
        }

        internal void Validate(ref string commandLineValue)
        {
            if (ArgumentType == typeof(SecureStringArgument) && Validators.Count() > 0)
            {
                throw new InvalidArgDefinitionException("Properties of type SecureStringArgument cannot be validated.  If your goal is to make the argument required then the[ArgRequired] attribute is not needed.  The SecureStringArgument is designed to prompt the user for a value only if your code asks for it after parsing.  If your code never reads the SecureString property then the user is never prompted and it will be treated as an optional parameter.  Although discouraged, if you really, really need to run custom logic against the value before the rest of your program runs then you can implement a custom ArgHook, override RunAfterPopulateProperty, and add your custom attribute to the SecureStringArgument property.");
            }

            foreach (var v in Validators)
            {
                if (v.ImplementsValidateAlways)
                {
                    try { v.ValidateAlways(this, ref commandLineValue); }
                    catch (NotImplementedException)
                    {
                        // TODO P0 - Test to make sure the old, PropertyInfo based validators properly work.
                        v.ValidateAlways(Source as PropertyInfo, ref commandLineValue);
                    }
                }
                else if (commandLineValue != null)
                {
                    v.Validate(Aliases.First(), ref commandLineValue);
                }
            }
        }

        internal void Revive(string commandLineValue)
        {
            if (ArgRevivers.CanRevive(ArgumentType) && commandLineValue != null)
            {
                try
                {
                    if (ArgumentType.IsEnum)
                    {
                        RevivedValue = ArgRevivers.ReviveEnum(ArgumentType, commandLineValue, IgnoreCase);
                    }
                    else
                    {
                        RevivedValue = ArgRevivers.Revive(ArgumentType, Aliases.First(), commandLineValue);
                    }
                }
                catch(TargetInvocationException ex)
                {
                    if (ex.InnerException != null && ex.InnerException is ArgException)
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                    else
                    {
                        if (ArgumentType.IsEnum)
                        {
                            throw new ArgException("'" + commandLineValue + "' is not a valid value for " + Aliases.First() + ". Available values are [" + string.Join(", ", Enum.GetNames(ArgumentType)) + "]", ex);
                        }
                        else
                        {
                            throw new ArgException(ex.Message, ex);
                        }
                    }
                }
                catch (ArgException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException is ArgException)
                    {
                        throw ex.InnerException;
                    }
                    else
                    {
                        if (ArgumentType.IsEnum) throw new ArgException("'" + commandLineValue + "' is not a valid value for " + Aliases.First() + ". Available values are [" + string.Join(", ", Enum.GetNames(ArgumentType)) + "]", ex);
                        else throw new ArgException(ex.Message, ex);
                    }
                }
            }
            else if (ArgRevivers.CanRevive(ArgumentType) && ArgumentType == typeof(SecureStringArgument))
            {
                RevivedValue = ArgRevivers.Revive(ArgumentType, Aliases.First(), commandLineValue);
            }
            else if (commandLineValue != null && ArgRevivers.CanRevive(ArgumentType))
            {
                throw new ArgException("Unexpected argument '" + Aliases.First() + "' with value '" + commandLineValue + "'");
            }
            else if (commandLineValue != null && ArgRevivers.CanRevive(ArgumentType) == false)
            {
                throw new InvalidArgDefinitionException("There is no reviver for type '" + ArgumentType.Name + '"');
            }
        }

        internal bool IsMatch(string key)
        {
            var ret = Aliases.Where(a => a.Equals(key, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).Count() > 0;
            return ret;
        }

        internal static bool IsArgument(PropertyInfo property)
        {
            if (property.HasAttr<ArgIgnoreAttribute>()) return false;
            if (CommandLineAction.IsActionImplementation(property)) return false;

            if (property.Name == Constants.ActionPropertyConventionName &&
                property.HasAttr<ArgPosition>() &&
                property.Attr<ArgPosition>().Position == 0 &&
                property.HasAttr<ArgRequired>())
            {
                return false;
            }

            return true;
        }

        internal static bool IsArgument(ParameterInfo parameter)
        {
            if (parameter.HasAttr<ArgIgnoreAttribute>()) return false;
            return true;
        }

        internal static void PopulateArguments(List<CommandLineArgument> arguments, ArgHook.HookContext context)
        {
            foreach (var argument in arguments)
            {
                argument.FindMatchingArgumentInRawParseData(context);
                argument.Populate(context);
            }
        }

        internal void Populate(ArgHook.HookContext context)
        {
            RunBeforePopulateProperty(context);

            if (RevivedValueOverride == null)
            {
                Validate(ref context.ArgumentValue);
                Revive(context.ArgumentValue);
            }
            else
            {
                RevivedValue = RevivedValueOverride;
                RevivedValueOverride = null;
            }

            RunAfterPopulateProperty(context);
        }

        internal static List<string> FindDefaultShortcuts(PropertyInfo info, List<string> knownShortcuts, bool ignoreCase)
        {
            List<string> ret = new List<string>();

            bool excludeName = info.Attrs<ArgShortcut>().Where(s => s.Policy == ArgShortcutPolicy.ShortcutsOnly).Count() > 0;

            if (excludeName == false)
            {
                knownShortcuts.Add(info.Name);

                if (CommandLineAction.IsActionImplementation(info) && info.Name.EndsWith(Constants.ActionArgConventionSuffix))
                {
                    ret.Add(info.Name.Substring(0, info.Name.Length - Constants.ActionArgConventionSuffix.Length));
                }
                else
                {
                    ret.Add(info.Name);
                }
            }

            var attrs = info.Attrs<ArgShortcut>();

            if (attrs.Count == 0)
            {
                var shortcut = GenerateShortcutAlias(info.Name, knownShortcuts, ignoreCase);
                if (shortcut != null)
                {
                    knownShortcuts.Add(shortcut);
                    ret.Add(shortcut);
                }

                return ret;
            }
            else
            {
                return ret;
            }
        }

        private void FindMatchingArgumentInRawParseData(ArgHook.HookContext context)
        {
            var match = from k in context.ParserData.ExplicitParameters.Keys where IsMatch(k) select k;

            if (match.Count() > 1)
            {
                throw new DuplicateArgException("Argument specified more than once: " + Aliases.First());
            }
            else if (match.Count() == 1)
            {
                var key = match.First();
                context.ArgumentValue = context.ParserData.ExplicitParameters[key];
                context.ParserData.ExplicitParameters.Remove(key);
            }
            else if (context.ParserData.ImplicitParameters.ContainsKey(Position))
            {
                var position = Position;
                context.ArgumentValue = context.ParserData.ImplicitParameters[position];
                context.ParserData.ImplicitParameters.Remove(position);
            }
            else
            {
                context.ArgumentValue = null;
            }
        }

        private static string GenerateShortcutAlias(string baseAlias, List<string> excluded, bool ignoreCase)
        {
            string shortcutVal = "";
            foreach (char c in baseAlias.Substring(0, baseAlias.Length - 1))
            {
                shortcutVal += c;
                if (excluded.Contains(shortcutVal, new CaseAwareStringComparer(ignoreCase)) == false)
                {
                    return shortcutVal;
                }
            }
            return null;
        }
        private static string GetTypeNameWithGenericsStripped(Type t)
        {
            string name = t.Name;
            int index = name.IndexOf('`');
            return index == -1 ? name : name.Substring(0, index);
        }
        private static string MapTypeName(Type t)
        {
            string ret;
            if (KnownTypeMappings.ContainsKey(t.Name))
            {
                ret = KnownTypeMappings[t.Name];
            }
            else
            {
                ret = t.Name.ToLower();
            }
            return ret;
        }
    }
}
