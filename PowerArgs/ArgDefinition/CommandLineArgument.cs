using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        /// <summary>
        /// The values that can be used as specifiers for this argument on the command line
        /// </summary>
        public List<string> Aliases { get; private set; }

        /// <summary>
        /// The validators that will execute when this argument is parsed
        /// </summary>
        public List<ArgValidator> Validators { get; private set; }

        /// <summary>
        /// The hooks that specifically target this argument
        /// </summary>
        public List<ArgHook> Hooks { get; private set; }

        /// <summary>
        /// The CLR type of this argument.
        /// </summary>
        public Type ArgumentType { get; private set; }

        /// <summary>
        /// Specifies whether or not the parser should ignore case when trying to find a match for this argument on the command line.  Defaults to true.
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// If this is a positional argument then set this value >= 0 and users can specify a value without specifying an argument alias.  Defaults to -1.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// The default value for this argument in the event it is optional and the user did not specify it.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// The description for this argument that appears in the auto generated usage.
        /// </summary>
        public string Description { get; set; }

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
        /// The first alias of this argument or null if no aliases are defined.
        /// </summary>
        public string DefaultAlias
        {
            get
            {
                return Aliases.FirstOrDefault();
            }
        }
        
        /// <summary>
        /// Returns true if the Validators collection contains an ArgRequired validator.
        /// </summary>
        public bool IsRequired
        {
            get
            {
                return Validators.Where(v => v is ArgRequired).Count() != 0;
            }
        }

        internal CommandLineArgument()
        {
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
            ret += "(Validators=" + Validators.Count + ")";
            ret += "(Hooks=" + Hooks.Count + ")";

            return ret;
        }

        internal static CommandLineArgument Create(PropertyInfo property)
        {
            var ret = PropertyInitializer.CreateInstance<CommandLineArgument>();
            ret.Description = property.HasAttr<ArgDescription>() ? property.Attr<ArgDescription>().Description : string.Empty;
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
            

            ret.Aliases.AddRange(CommandLineArgumentsDefinition.FindAliases(property));
            ret.Validators.AddRange(property.Attrs<ArgValidator>().OrderByDescending(val => val.Priority));
            ret.Hooks.AddRange(property.Attrs<ArgHook>());

            return ret;
        }

        internal static CommandLineArgument Create(ParameterInfo parameter)
        {
            var ret = PropertyInitializer.CreateInstance<CommandLineArgument>();
            ret.Position = parameter.Position;
            ret.ArgumentType = parameter.ParameterType;
            ret.Source = parameter;
            ret.Description = parameter.HasAttr<ArgDescription>() ? parameter.Attr<ArgDescription>().Description : string.Empty;
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
            ret.Validators.AddRange(parameter.Attrs<ArgValidator>().OrderByDescending(val => val.Priority));
            ret.Hooks.AddRange(parameter.Attrs<ArgHook>());

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
            if (ArgumentType == typeof(SecureStringArgument) && Validators.Count > 0)
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
            else if (commandLineValue != null)
            {
                throw new ArgException("Unexpected argument '" + Aliases.First() + "' with value '" + commandLineValue + "'");
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
                argument.RunBeforePopulateProperty(context);
                argument.Validate(ref context.ArgumentValue);
                argument.Revive(context.ArgumentValue);
                argument.RunAfterPopulateProperty(context);
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
    }
}
