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

        [ThreadStatic]
        private static CommandLineArgumentsDefinition _ambientDefinition;

        /// <summary>
        /// Gets the last definition parsed on the current thread or null if none was parsed.
        /// </summary>
        /// <returns>last definition parsed on the current thread or null if none was parsed</returns>
        public static CommandLineArgumentsDefinition GetAmbientDefinition()
        {
            return _ambientDefinition;
        }

        private Args() { }

        /// <summary>
        /// PowerArgs will manually search the assembly you provide for any custom type revivers.  If you don't specify an
        /// assembly then the assembly that calls this function will automatically be searched.
        /// </summary>
        /// <param name="a">The assembly to search or null if you want PowerArgs to search the assembly that's calling into this function.</param>
        public static void SearchAssemblyForRevivers(Assembly a = null)
        {
            a = a ?? Assembly.GetCallingAssembly();
            ArgRevivers.SearchAssemblyForRevivers(a);
        }

        /// <summary>
        /// Converts a single string that represents a command line to be executed into a string[], 
        /// accounting for quoted arguments that may or may not contain spaces.
        /// </summary>
        /// <param name="commandLine">The raw arguments as a single string</param>
        /// <returns>a converted string array with the arguments properly broken up</returns>
        public static string[] Convert(string commandLine)
        {
            List<string> ret = new List<string>();
            string currentArg = string.Empty;
            bool insideDoubleQuotes = false;

            for(int i = 0; i < commandLine.Length;i++)
            {
                var c = commandLine[i];

                if (insideDoubleQuotes && c == '"')
                {
                    ret.Add(currentArg);
                    currentArg = string.Empty;
                    insideDoubleQuotes = !insideDoubleQuotes;
                }
                else if (!insideDoubleQuotes && c == ' ')
                {
                    if (currentArg.Length > 0)
                    {
                        ret.Add(currentArg);
                        currentArg = string.Empty;
                    }
                }
                else if (c == '"')
                {
                    insideDoubleQuotes = !insideDoubleQuotes;
                }
                else if (c == '\\' && i < commandLine.Length - 1 && commandLine[i + 1] == '"')
                {
                    currentArg += '"';
                }
                else
                {
                    currentArg += c;
                }
            }

            if (currentArg.Length > 0)
            {
                ret.Add(currentArg);
            }

            return ret.ToArray();
        }

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
        /// If properties on the given object contain default value attributes then this method will initalize those properties with
        /// the right defaults
        /// </summary>
        /// <param name="o">the object to initialize</param>
        public static void InitializeDefaults(object o)
        {
            if(o == null)
            {
                throw new ArgumentNullException("o cannot be null");
            }

            var def = new CommandLineArgumentsDefinition(o.GetType());
            var context = new ArgHook.HookContext();
            context.Definition = def;
            context.Args = o;

            foreach(var arg in def.Arguments)
            {
                context.ArgumentValue = null;
                context.CurrentArgument = arg;
                context.RevivedProperty = null;
                if(arg.HasDefaultValue == false)
                {
                    continue;
                }

                arg.Populate(context);
            }

            def.SetPropertyValues(o);
        }

        /// <summary>
        /// Parses the given arguments using a command line arguments definition.  
        /// </summary>
        /// <param name="definition">The definition that defines a set of command line arguments and/or actions.</param>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns></returns>
        public static ArgAction ParseAction(CommandLineArgumentsDefinition definition, params string[] args)
        {
            ArgAction ret = Execute(() =>
            {
                Args instance = new Args();
                return instance.ParseInternal(definition, args);
            });

            return ret;
        }

        /// <summary>
        /// Parses the args for the given definition and then calls the Main() method defined by the type.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static ArgAction InvokeMain(CommandLineArgumentsDefinition definition, params string[] args)
        {
            return REPL.DriveREPL<ArgAction>(definition.Metadata.Meta<TabCompletion>(), (a) =>
            {
                return Execute<ArgAction>(() =>
                {
                    Args instance = new Args();
                    var result = instance.ParseInternal(definition, a);
                    if (result.HandledException == null)
                    {
                        result.Context.RunBeforeInvoke();
                        result.Value.InvokeMainMethod();
                        result.Context.RunAfterInvoke();
                    }
                    return result;
                });
            }, args);
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
            return REPL.DriveREPL<ArgAction>(definition.Hooks.Where(h => h is TabCompletion).Select(h => h as TabCompletion).SingleOrDefault(), (a) =>
            {
                return Execute<ArgAction>(() =>
                {
                    Args instance = new Args();
                    var result = instance.ParseInternal(definition, a);
                    if (result.HandledException == null)
                    {
                        result.Invoke();
                    }
                    return result;
                });
            }, args);
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
        /// Creates a new instance of T and populates it's properties based on the given arguments.
        /// </summary>
        /// <typeparam name="T">The argument scaffold type.</typeparam>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>A new instance of T with all of the properties correctly populated</returns>
        public static T Parse<T>(params string[] args) where T : class
        {
            return Parse(typeof(T), args) as T;
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
            return Strongify<T>(ParseAction(new CommandLineArgumentsDefinition(typeof(T)), args));
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
        /// Parses the args for the given scaffold type and then calls the Main() method defined by the type.
        /// </summary>
        /// <param name="t">The argument scaffold type.</param>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>The raw result of the parse with metadata about the specified action.</returns>
        public static ArgAction InvokeMain(Type t, params string[] args)
        {
            return InvokeMain(new CommandLineArgumentsDefinition(t), args);
        }

        /// <summary>
        /// Parses the args for the given scaffold type and then calls the Main() method defined by the type.
        /// </summary>
        /// <typeparam name="T">The argument scaffold type.</typeparam>
        /// <param name="args">The command line arguments to parse</param>
        /// <returns>The raw result of the parse with metadata about the specified action.</returns>
        public static ArgAction<T> InvokeMain<T>(params string[] args)
        {
            return Strongify<T>(InvokeMain(new CommandLineArgumentsDefinition(typeof(T)), args));
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
            return Strongify<T>(InvokeAction(new CommandLineArgumentsDefinition(typeof(T)), args));
        }

        /// <summary>
        /// Parses the given arguments using a command line arguments definition. The values will be populated within
        /// the definition.
        /// </summary>
        /// <param name="definition">The definition that defines a set of command line arguments and/or actions.</param>
        /// <param name="args">The command line arguments to parse</param>
        public static ArgAction Parse(CommandLineArgumentsDefinition definition, params string[] args)
        {
            return ParseAction(definition, args);
        }

        private static T Execute<T>(Func<T> argsProcessingCode) where T : class
        {
            ArgHook.HookContext.Current = new ArgHook.HookContext();

            try
            {
                return argsProcessingCode();
            }
            catch (ArgCancelProcessingException ex)
            {
                return CreateEmptyResult<T>(ArgHook.HookContext.Current, cancelled: true);
            }
            catch (ArgException ex)
            {
                ex.Context = ArgHook.HookContext.Current;
                var definition = ArgHook.HookContext.Current.Definition;
                if (definition.ExceptionBehavior.Policy == ArgExceptionPolicy.StandardExceptionHandling)
                {
                    return DoStandardExceptionHandling<T>(ex, ArgHook.HookContext.Current, definition);
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

        private static T DoStandardExceptionHandling<T>(ArgException ex, ArgHook.HookContext context, CommandLineArgumentsDefinition definition) where T : class
        {
            Console.WriteLine(ex.Message);

            if (definition.ExceptionBehavior.UsageTemplateFile != null)
            {
                var template = File.ReadAllText(definition.ExceptionBehavior.UsageTemplateFile);
                ArgUsage.GenerateUsageFromTemplate(definition, template).Write();
            }
            else
            {
                UsageTemplateProvider.GetUsage(definition.ExceptionBehavior.UsageTemplateProviderType, definition).Write();
            }

            return CreateEmptyResult<T>(context, ex);
        }

        private static T CreateEmptyResult<T>(ArgHook.HookContext context, ArgException ex = null, bool cancelled = false)
        {
            ArgAction ret = new ArgAction();

            if (typeof(T) == typeof(ArgAction))
            {
                ret = new ArgAction();
            }
            else if (typeof(T).IsSubclassOf(typeof(ArgAction)))
            {
                ret = Activator.CreateInstance(typeof(T)) as ArgAction;
            }
            else
            {
                return default(T);
            }

            ret.HandledException = ex;
            ret.Definition = context.Definition;
            ret.Context = context;
            ret.Cancelled = cancelled;
            return (T)((object)ret);
        }

        private static ArgAction<T> Strongify<T>(ArgAction weak)
        {
            return new ArgAction<T>()
            {
                Args = (T)weak.Value,
                ActionArgs = weak.ActionArgs,
                ActionArgsProperty = weak.ActionArgsProperty,
                ActionParameters = weak.ActionParameters,
                ActionArgsMethod = weak.ActionArgsMethod,
                HandledException = weak.HandledException,
                Definition = weak.Definition,
                Context = weak.Context,
                Cancelled = weak.Cancelled,
            };
        }

        private ArgAction ParseInternal(CommandLineArgumentsDefinition definition, string[] input)
        {
            // TODO - Validation should be consistently done against the definition, not against the raw type
            if (definition.ArgumentScaffoldType != null) ValidateArgScaffold(definition.ArgumentScaffoldType);

            var context = ArgHook.HookContext.Current;
            context.Definition = definition;
            _ambientDefinition = definition;

            definition.Validate(context);

            if (definition.ArgumentScaffoldType != null) context.Args = Activator.CreateInstance(definition.ArgumentScaffoldType);
            context.CmdLineArgs = input;

            context.RunBeforeParse();
            context.ParserData = ArgParser.Parse(definition, context.CmdLineArgs);

            var actionToken = context.CmdLineArgs.FirstOrDefault();
            var actionQuery = context.Definition.Actions.Where(a => a.IsMatch(actionToken));

            if(actionQuery.Count() == 1)
            {
                context.SpecifiedAction = actionQuery.First();
            }
            else if(actionQuery.Count() > 1)
            {
                throw new InvalidArgDefinitionException("There are multiple actions that match argument '" + actionToken + "'");
            }

            context.RunBeforePopulateProperties();
            CommandLineArgument.PopulateArguments(context.Definition.Arguments, context);
            context.Definition.SetPropertyValues(context.Args);

            object actionArgs = null;
            object[] actionParameters = null;

            if (context.SpecifiedAction == null && context.Definition.Actions.Count > 0)
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
            else if (context.SpecifiedAction != null)
            {
                PropertyInfo actionProp = null;
                if (context.Definition.ArgumentScaffoldType != null)
                {
                    actionProp = ArgAction.GetActionProperty(context.Definition.ArgumentScaffoldType);
                }

                if (actionProp != null)
                {
                    actionProp.SetValue(context.Args, context.SpecifiedAction.Aliases.First(), null);
                }

                context.ParserData.ImplicitParameters.Remove(0);
                CommandLineArgument.PopulateArguments(context.SpecifiedAction.Arguments, context);
            }

            context.RunAfterPopulateProperties();

            if(context.SpecifiedAction != null)
            {
                actionArgs = context.SpecifiedAction.PopulateArguments(context.Args, ref actionParameters);
            }

            if (context.Definition.Metadata.HasMeta<AllowUnexpectedArgs>() == false)
            {
                if (context.ParserData.ImplicitParameters.Count > 0)
                {
                    throw new UnexpectedArgException("Unexpected unnamed argument: " + context.ParserData.ImplicitParameters.First().Value);
                }

                if (context.ParserData.ExplicitParameters.Count > 0)
                {
                    throw new UnexpectedArgException("Unexpected named argument: " + context.ParserData.ExplicitParameters.First().Key);
                }
            }
            else
            {
                definition.UnexpectedExplicitArguments = context.ParserData.ExplicitParameters;
                definition.UnexpectedImplicitArguments = context.ParserData.ImplicitParameters;   
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

            if (context.SpecifiedAction != null)
            {
                if (context.SpecifiedAction.Source is PropertyInfo) actionArgsPropertyInfo = context.SpecifiedAction.Source as PropertyInfo;
                else if (context.SpecifiedAction.Source is MethodInfo) actionArgsPropertyInfo = new ArgActionMethodVirtualProperty(context.SpecifiedAction.Source as MethodInfo);
            }

            return new ArgAction()
            {
                Value = context.Args,
                ActionArgs = actionArgs,
                ActionParameters = actionParameters,
                ActionArgsProperty = actionArgsPropertyInfo,
                ActionArgsMethod = context.SpecifiedAction != null ? context.SpecifiedAction.ActionMethod : null,
                Definition = context.Definition,
                Context = context,
            };
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

                // This check happens in the CommandLineArgumentsDefinition validation method and should not be repeated here.  Leaving the code commented while this bakes, but this code
                // should be removable in the future.
                //if (ArgRevivers.CanRevive(prop.PropertyType) == false)
                //{
                //    throw new InvalidArgDefinitionException("There is no reviver for type " + prop.PropertyType.Name + ". Offending Property: " + prop.DeclaringType.Name + "." + prop.Name);
                //}

                if (prop.PropertyType.IsEnum)
                {
                    prop.PropertyType.ValidateNoDuplicateEnumShortcuts(ignoreCase);
                }

                var attrs = prop.Attrs<ArgShortcut>();
                var noShortcutsAllowed = attrs.Where(a => a.Policy == ArgShortcutPolicy.NoShortcut).Count() != 0;
                var shortcutsOnly = attrs.Where(a => a.Policy == ArgShortcutPolicy.ShortcutsOnly).Count() != 0;
                var actualShortcutValues = attrs.Where(a => a.Policy == ArgShortcutPolicy.Default && a.Shortcut != null).Count() != 0;

                if (noShortcutsAllowed && shortcutsOnly) throw new InvalidArgDefinitionException("You cannot specify a policy of NoShortcut and another policy of ShortcutsOnly.");
                if (noShortcutsAllowed && actualShortcutValues) throw new InvalidArgDefinitionException("You cannot specify a policy of NoShortcut and then also specify shortcut values via another attribute.");
                if (shortcutsOnly && actualShortcutValues == false) throw new InvalidArgDefinitionException("You specified a policy of ShortcutsOnly, but did not specify any shortcuts by adding another ArgShortcut attrivute.");
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
