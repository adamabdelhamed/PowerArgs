using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// The main entry point for PowerArgs that includes the public parsing functions such as Parse, ParseAction, and InvokeAction.
    /// </summary>
    public class Args
    {
        [ThreadStatic]
        private static Dictionary<Type, object> _ambientArgs;

        private static Dictionary<Type, object> AmbientArgs
        {
            get
            {
                if (_ambientArgs == null) _ambientArgs = new Dictionary<Type, object>();
                return _ambientArgs;
            }
        }

        private Args() { }

        /// <summary>
        /// Gets the last instance of this type of argument that was parsed on the current thread
        /// or null if PowerArgs did not parse an object of this type.
        /// </summary>
        /// <typeparam name="T">The scaffold type for your arguments</typeparam>
        /// <returns>the last instance of this type of argument that was parsed on the current thread</returns>
        public static T GetAmbientArgs<T>() where T : class
        {
            return (T)GetAmbientArgs(typeof(T));
        }

        /// <summary>
        /// Gets the last instance of this type of argument that was parsed on the current thread
        /// or null if PowerArgs did not parse an object of this type.
        /// </summary>
        /// <param name="t">The scaffold type for your arguments</param>
        /// <returns>the last instance of this type of argument that was parsed on the current thread</returns>
        public static object GetAmbientArgs(Type t)
        {
            object ret;
            if (AmbientArgs.TryGetValue(t, out ret))
            {
                return ret;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new instance of T and populates it's properties based on the given arguments.
        /// If T correctly implements the heuristics for Actions (or sub commands) then the complex property
        /// that represents the options of a sub command are also populated.
        /// </summary>
        /// <typeparam name="T">The argument scaffold type.</typeparam>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>The raw result of the parse with metadata about the specified action.</returns>
        public static ArgAction<T> ParseAction<T>(params string[] args)
        {
            Args instance = new Args();
            return instance.ParseInternal<T>(args);
        }

        /// <summary>
        /// Creates a new instance of the given type and populates it's properties based on the given arguments.
        /// If the type correctly implements the heuristics for Actions (or sub commands) then the complex property
        /// that represents the options of a sub command are also populated.
        /// </summary>
        /// <param name="t">The argument scaffold type.</param>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>The raw result of the parse with metadata about the specified action.</returns>
        public static ArgAction ParseAction(Type t, params string[] args)
        {
            return ParseAction(new CommandLineArgumentsDefinition(t), args);
        }

        /// <summary>
        /// Parses the given arguments using a command line arguments definition.  
        /// </summary>
        /// <param name="definition">The definition that defines a set of command line arguments and/or actions.</param>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns></returns>
        public static ArgAction ParseAction(CommandLineArgumentsDefinition definition, params string[] args)
        {
            Args instance = new Args();
            return instance.ParseInternal(definition, args);
        }

        /// <summary>
        /// Parses the args for the given scaffold type and then calls the Main() method defined by the type.
        /// </summary>
        /// <param name="t">The argument scaffold type.</param>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>The raw result of the parse with metadata about the specified action.</returns>
        public static ArgAction InvokeMain(Type t, params string[] args)
        {
            var ret = REPL.DriveREPL<ArgAction>(t.Attr<TabCompletion>(), (a) =>
            {
                var result = ParseAction(t, a);
                if (result.HandledException == null) result.Value.InvokeMainMethod();
                return result;
            }
            , args);

            return ret;
        }

        /// <summary>
        /// Parses the args for the given scaffold type and then calls the Main() method defined by the type.
        /// </summary>
        /// <typeparam name="T">The argument scaffold type.</typeparam>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>The raw result of the parse with metadata about the specified action.</returns>
        public static ArgAction<T> InvokeMain<T>(params string[] args)
        {

            var ret = REPL.DriveREPL<ArgAction<T>>(typeof(T).Attr<TabCompletion>(), (a) => 
            {
                var result = ParseAction<T>(a);
                if (result.HandledException == null) result.Value.InvokeMainMethod();
                return result;
            }
            , args);

            return ret;
        }

        /// <summary>
        /// Creates a new instance of T and populates it's properties based on the given arguments. T must correctly
        /// implement the heuristics for Actions (or sub commands) because this method will not only detect the action
        /// specified on the command line, but will also find and execute the method that implements the action.
        /// </summary>
        /// <typeparam name="T">The argument scaffold type that must properly implement at least one action.</typeparam>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>The raw result of the parse with metadata about the specified action.  The action is executed before returning.</returns>
        public static ArgAction<T> InvokeAction<T>(params string[] args)
        {
            var ret = REPL.DriveREPL<ArgAction<T>>(typeof(T).Attr<TabCompletion>(), (a) =>
            {
                var result = ParseAction<T>(a);
                if (result.HandledException == null) result.Invoke();
                return result;
            }
            , args);

            return ret;
        }

        /// <summary>
        /// Parses the given arguments using a command line arguments definition.  Then, invokes the action
        /// that was specified.  
        /// </summary>
        /// <param name="definition">The definition that defines a set of command line arguments and actions.</param>
        /// <param name="args"></param>
        /// <returns>The raw result of the parse with metadata about the specified action.  The action is executed before returning.</returns>
        public static ArgAction InvokeAction(CommandLineArgumentsDefinition definition, params string[] args)
        {
            var ret = REPL.DriveREPL<ArgAction>(definition.Hooks.Where(h => h is TabCompletion).Select(h => h as TabCompletion).SingleOrDefault(), (a) =>
            {
                var result = ParseAction(definition, a);
                if (result.HandledException == null) result.Invoke();
                return result;
            }
            , args);

            return ret;
        }

        /// <summary>
        /// Creates a new instance of T and populates it's properties based on the given arguments.
        /// </summary>
        /// <typeparam name="T">The argument scaffold type.</typeparam>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>A new instance of T with all of the properties correctly populated</returns>
        public static T Parse<T>(params string[] args)
        {
            return ParseAction<T>(args).Args;
        }

        /// <summary>
        /// Creates a new instance of the given type and populates it's properties based on the given arguments.
        /// </summary>
        /// <param name="t">The argument scaffold type</param>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>A new instance of the given type with all of the properties correctly populated</returns>
        public static object Parse(Type t, params string[] args)
        {
            return ParseAction(t, args).Value;
        }

        /// <summary>
        /// Parses the given arguments using a command line arguments definition. The values will be populated within
        /// the definition.
        /// </summary>
        /// <param name="definition">The definition that defines a set of command line arguments and/or actions.</param>
        /// <param name="args">The command line arguments to parse</param>
        public static void Parse(CommandLineArgumentsDefinition definition, params string[] args)
        {
            ParseAction(definition, args);
        }

        private ArgAction<T> ParseInternal<T>(string[] input)
        {
            var weak = ParseInternal(new CommandLineArgumentsDefinition(typeof(T)), input);
            return new ArgAction<T>()
            {
                Args = (T)weak.Value,
                ActionArgs = weak.ActionArgs,
                ActionArgsProperty = weak.ActionArgsProperty,
                ActionParameters = weak.ActionParameters,
                ActionArgsMethod = weak.ActionArgsMethod,
                HandledException = weak.HandledException,
                Definition = weak.Definition,
            };
        }

        private ArgAction ParseInternal(CommandLineArgumentsDefinition definition, string[] input)
        {
            try
            {
                // TODO P0 - Validation should be consistently done against the definition, not against the raw type
                if (definition.ArgumentScaffoldType != null) ValidateArgScaffold(definition.ArgumentScaffoldType);
                definition.Validate();

                var context = new ArgHook.HookContext();
                context.Definition = definition;
                if (definition.ArgumentScaffoldType != null) context.Args = Activator.CreateInstance(definition.ArgumentScaffoldType);
                context.CmdLineArgs = input;
                ArgHook.HookContext.Current = context;

                context.RunBeforeParse();
                context.ParserData = ArgParser.Parse(context.CmdLineArgs);

                context.RunBeforePopulateProperties();
                CommandLineArgument.PopulateArguments(context.Definition.Arguments, context);
                context.Definition.SetPropertyValues(context.Args);

                object actionArgs = null;
                object[] actionParameters = null;
                var specifiedAction = context.Definition.Actions.Where(a => a.IsMatch(context.CmdLineArgs.FirstOrDefault())).SingleOrDefault();
                if (specifiedAction == null && context.Definition.Actions.Count > 0)
                {
                    if (context.CmdLineArgs.FirstOrDefault() == null)
                    {
                        throw new MissingArgException("No action was specified");
                    }
                    else
                    {
                        throw new UnknownActionArgException(string.Format("Unknown action: '{0}'", context.CmdLineArgs.FirstOrDefault()));
                    }
                }
                else if (specifiedAction != null)
                {
                    foreach (var action in context.Definition.Actions)
                    {
                        action.IsSpecifiedAction = false;
                    }
                    specifiedAction.IsSpecifiedAction = true;
                    
                    PropertyInfo actionProp = null;
                    if (context.Definition.ArgumentScaffoldType != null)
                    {
                        actionProp = ArgAction.GetActionProperty(context.Definition.ArgumentScaffoldType);
                    }

                    if (actionProp != null)
                    {
                        actionProp.SetValue(context.Args, specifiedAction.Aliases.First(), null);
                    }

                    context.ParserData.ImplicitParameters.Remove(0);
                    CommandLineArgument.PopulateArguments(specifiedAction.Arguments, context);
                    actionArgs = specifiedAction.PopulateArguments(context.Args, ref actionParameters);
                }

                context.RunAfterPopulateProperties();

                if (context.ParserData.ImplicitParameters.Count > 0)
                {
                    throw new UnexpectedArgException("Unexpected unnamed argument: " + context.ParserData.ImplicitParameters.First().Value);
                }

                if (context.ParserData.ExplicitParameters.Count > 0)
                {
                    throw new UnexpectedArgException("Unexpected named argument: " + context.ParserData.ExplicitParameters.First().Key);
                }

                if (definition.ArgumentScaffoldType != null)
                {
                    if (AmbientArgs.ContainsKey(definition.ArgumentScaffoldType))
                    {
                        AmbientArgs[definition.ArgumentScaffoldType] = context.Args;
                    }
                    else
                    {
                        AmbientArgs.Add(definition.ArgumentScaffoldType, context.Args);
                    }
                }

                PropertyInfo actionArgsPropertyInfo = null;

                if(specifiedAction != null)
                {
                    if(specifiedAction.Source is PropertyInfo) actionArgsPropertyInfo = specifiedAction.Source as PropertyInfo;
                    else if(specifiedAction.Source is MethodInfo) actionArgsPropertyInfo = new ArgActionMethodVirtualProperty(specifiedAction.Source as MethodInfo);
                }

                return new ArgAction()
                {
                    Value = context.Args,
                    ActionArgs = actionArgs,
                    ActionParameters = actionParameters,
                    ActionArgsProperty = actionArgsPropertyInfo,
                    ActionArgsMethod = specifiedAction != null ? specifiedAction.ActionMethod : null,
                    Definition = context.Definition,
                };
            }
            catch (ArgException ex)
            {
                if (definition.ExceptionBehavior.Policy == ArgExceptionPolicy.StandardExceptionHandling)
                {
                    Console.WriteLine(ex.Message);

                    ArgUsage.GetStyledUsage(definition, definition.ExceptionBehavior.ExeName, new ArgUsageOptions
                    {
                        ShowPosition = definition.ExceptionBehavior.ShowPositionColumn,
                        ShowType = definition.ExceptionBehavior.ShowTypeColumn,
                        ShowPossibleValues = definition.ExceptionBehavior.ShowPossibleValues,

                    }).Write();

                    return new ArgAction()
                    {
                        Value = null,
                        ActionArgs = null,
                        ActionArgsProperty = null,
                        HandledException = ex,
                        Definition = definition,
                    };
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                ArgHook.HookContext.Current = null;
            }
        }

        private void ValidateArgScaffold(Type t, List<string> shortcuts = null, Type parentType = null)
        {
            /*
             * Today, this validates the following:
             * 
             *     - IgnoreCase can't be different on parent and child scaffolds.
             *     - No collisions on shortcut values for properties and enum values
             *     - No reviver for type
             * 
             */

            if (parentType != null)
            {
                if(parentType.HasAttr<ArgIgnoreCase>() ^ t.HasAttr<ArgIgnoreCase>())
                {
                    throw new InvalidArgDefinitionException("If you specify the " + typeof(ArgIgnoreCase).Name + " attribute on your base type then you must also specify it on each action type.");
                }
                else if (parentType.HasAttr<ArgIgnoreCase>() && parentType.Attr<ArgIgnoreCase>().IgnoreCase != t.Attr<ArgIgnoreCase>().IgnoreCase)
                {
                    throw new InvalidArgDefinitionException("If you specify the " + typeof(ArgIgnoreCase).Name + " attribute on your base and acton types then they must be configured to use the same value for IgnoreCase.");
                }
            }

            if (t.Attrs<ArgIgnoreCase>().Count > 1) throw new InvalidArgDefinitionException("An attribute that is or derives from " + typeof(ArgIgnoreCase).Name+" was specified on your type more than once");


            var actionProp = ArgAction.GetActionProperty(t);
            shortcuts = shortcuts ?? new List<string>();
            bool ignoreCase = true;
            if (t.HasAttr<ArgIgnoreCase>() && t.Attr<ArgIgnoreCase>().IgnoreCase == false) ignoreCase = false;

            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (CommandLineAction.IsActionImplementation(prop)) continue;

                if (ArgRevivers.CanRevive(prop.PropertyType) == false)
                {
                    throw new InvalidArgDefinitionException("There is no reviver for type " + prop.PropertyType.Name + ". Offending Property: " + prop.DeclaringType.Name + "." + prop.GetArgumentName());
                }

                if (prop.PropertyType.IsEnum)
                {
                    prop.PropertyType.ValidateNoDuplicateEnumShortcuts(ignoreCase);
                }

                prop.ValidateNoConflictingShortcutPolicies();
                var shortcutsForProperty = ArgShortcut.GetShortcutsInternal(prop).ToArray().ToList();
                if(shortcutsForProperty.Contains(prop.GetArgumentName()) == false)
                {
                    shortcutsForProperty.Add(prop.GetArgumentName());
                }

                foreach (var shortcutVal in shortcutsForProperty)
                {
                    string shortcut = shortcutVal;
                    if (ignoreCase && shortcut != null) shortcut = shortcut.ToLower();

                    if (shortcuts.Contains(shortcut))
                    {
                        throw new InvalidArgDefinitionException("Duplicate arg options with shortcut '" + shortcut + "'.  Keep in mind that shortcuts are not case sensitive unless you use the [ArgIgnoreCase(false)] attribute.  For example, Without this attribute the shortcuts '-a' and '-A' would cause this exception.");
                    }
                    else
                    {
                        shortcuts.Add(shortcut);
                    }
                }
            }

            if (actionProp != null)
            {
                foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (CommandLineAction.IsActionImplementation(prop))
                    {
                        ArgAction.ResolveMethod(t,prop);
                        ValidateArgScaffold(prop.PropertyType, shortcuts.ToArray().ToList(), t);
                    }
                }
            }

            foreach (var actionMethod in t.GetActionMethods())
            {
                if(actionMethod.GetParameters().Length == 0)continue;

                ValidateArgScaffold(actionMethod.GetParameters()[0].ParameterType, shortcuts.ToArray().ToList(), t);
            }
        }
    }
}
