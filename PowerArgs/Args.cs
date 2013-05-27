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
        private Args() { } 

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
            Args instance = new Args();
            return instance.ParseInternal(t, args);
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
            var action = Args.ParseAction<T>(args);
            action.Invoke();
            return action;
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

        private ArgAction<T> ParseInternal<T>(string[] input)
        {
            var weak = ParseInternal(typeof(T), input);
            return new ArgAction<T>()
            {
                Args = (T)weak.Value,
                ActionArgs = weak.ActionArgs,
                ActionArgsProperty = weak.ActionArgsProperty
            };
        }

        private ArgAction ParseInternal(Type t, string[] input)
        {
            ArgShortcut.RegisterShortcuts(t);
            ValidateArgScaffold(t);

            var context = new ArgHook.HookContext();
            context.Args = Activator.CreateInstance(t);
            context.CmdLineArgs = input;

            t.RunBeforeParse(context);
            var specifiedActionProperty = FindSpecifiedAction(t, ref context.CmdLineArgs);
            context.ParserData = ArgParser.Parse(context.CmdLineArgs);
            PopulateProperties(context);

            if (specifiedActionProperty != null)
            {
                var actionPropertyValue = Activator.CreateInstance(specifiedActionProperty.PropertyType);
                var toRestore = context.Args;
                context.Args = actionPropertyValue;
                PopulateProperties(context);
                context.Args = toRestore;
                specifiedActionProperty.SetValue(context.Args, actionPropertyValue, null);
            }

            if (context.ParserData.ImplicitParameters.Count > 0)
            {
                throw new UnexpectedArgException("Unexpected unnamed argument: " + context.ParserData.ImplicitParameters.First().Value);
            }

            if (context.ParserData.ExplicitParameters.Count > 0)
            {
                throw new UnexpectedArgException("Unexpected named argument: " + context.ParserData.ExplicitParameters.First().Key);
            }

            return new ArgAction()
            {
                Value = context.Args,
                ActionArgs = specifiedActionProperty != null ? specifiedActionProperty.GetValue(context.Args, null) : null,
                ActionArgsProperty = specifiedActionProperty
            };
        }

        private PropertyInfo FindSpecifiedAction(Type t, ref string[] args)
        {
            var actionProperty = ArgAction.GetActionProperty(t);
            if (actionProperty == null) return null;

            var specifiedAction = args.Length > 0 ? args[0] : null;

            if (actionProperty.Attr<ArgRequired>().PromptIfMissing && args.Length == 0)
            {
                actionProperty.Attr<ArgRequired>().ValidateAlways(actionProperty, ref specifiedAction);
                args = new string[] { specifiedAction };
            }

            if (specifiedAction == null) return null;

            var actionArgProperty = (from p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     where p.MatchesSpecifiedAction(specifiedAction)
                                     select p).SingleOrDefault();

            if (actionArgProperty == null) throw new UnknownActionArgException("Unknown Action: " + specifiedAction);

            return actionArgProperty;
        }

        private void PopulateProperties(ArgHook.HookContext context)
        {
            context.Args.GetType().RunBeforePopulateProperties(context);
            foreach (PropertyInfo prop in context.Args.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var match = from k in context.ParserData.ExplicitParameters.Keys
                            where prop.MatchesSpecifiedArg(k)
                            select k;

                if (match.Count() > 1) throw new DuplicateArgException("Argument specified more than once: "+prop.GetArgumentName());

                if (match.Count() == 1)
                {
                    var key = match.First();
                    context.ArgumentValue = context.ParserData.ExplicitParameters[key];
                    context.ParserData.ExplicitParameters.Remove(key);
                }
                else
                {
                    if (prop.HasAttr<ArgPosition>() && context.ParserData.ImplicitParameters.ContainsKey(prop.Attr<ArgPosition>().Position))
                    {
                        var position = prop.Attr<ArgPosition>().Position;
                        context.ArgumentValue = context.ParserData.ImplicitParameters[position];
                        context.ParserData.ImplicitParameters.Remove(position);
                    }
                    else
                    {
                        context.ArgumentValue = null;
                    }
                }
 

                context.Property = prop;

                prop.RunBeforePopulateProperty(context);

                bool shouldValidateAndRevive = true;
                if (prop.Attr<ArgIgnoreAttribute>() != null) shouldValidateAndRevive = false;
                if (prop.IsActionArgProperty() && ArgAction.GetActionProperty(context.Args.GetType()) != null) shouldValidateAndRevive = false;

                if (shouldValidateAndRevive)
                {
                    prop.Validate(context);
                    prop.Revive(context.Args, context);
                }

                prop.RunAfterPopulateProperty(context);
            }
            context.Args.GetType().RunAfterPopulateProperties(context);
        }

        private void ValidateArgScaffold<T>()
        {
            ValidateArgScaffold(typeof(T));
        }

        private void ValidateArgScaffold(Type t, List<string> shortcuts = null, Type parentType = null)
        {
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
                if (prop.IsActionArgProperty() && actionProp != null) continue;

                if (ArgRevivers.CanRevive(prop.PropertyType) == false)
                {
                    throw new InvalidArgDefinitionException("There is no reviver for type " + prop.PropertyType.Name + ". Offending Property: " + prop.DeclaringType.Name + "." + prop.GetArgumentName());
                }

                var shortcut = ArgShortcut.GetShortcut(prop);

                if (ignoreCase && shortcut != null) shortcut = shortcut.ToLower();
                
                if (shortcut != null && shortcuts.Contains(shortcut))
                {
                    throw new InvalidArgDefinitionException("Duplicate arg options with shortcut '" + ArgShortcut.GetShortcut(prop) + "'.  Keep in mind that shortcuts are not case sensitive unless you use the [ArgIgnoreCase(false)] attribute.  For example, Without this attribute the shortcuts '-a' and '-A' would cause this exception.");
                }
                else if(shortcut != null)
                {
                    shortcuts.Add(shortcut);
                }
            }

            if (actionProp != null)
            {
                foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (prop.IsActionArgProperty())
                    {
                        ArgAction.ResolveMethod(t,prop);
                        ValidateArgScaffold(prop.PropertyType, shortcuts.ToArray().ToList(), t);
                    }
                }
            }
        }
    }
}
