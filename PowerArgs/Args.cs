using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace PowerArgs
{
    public class Args
    {
        private Args() { }

        public static ArgAction<T> ParseAction<T>(params string[] args)
        {
            Args instance = new Args();
            return instance.ParseInternal<T>(args);
        }

        public static ArgAction<T> InvokeAction<T>(params string[] args)
        {
            var action = Args.ParseAction<T>(args);
            action.Invoke();
            return action;
        }

        public static T Parse<T>(params string[] args)
        {
            return ParseAction<T>(args).Args;
        }

        private ArgAction<T> ParseInternal<T>(string[] input)
        {
            ArgShortcut.RegisterShortcuts(typeof(T));
            ValidateArgScaffold<T>();

            var context = new ArgHook.HookContext();
            context.Args = Activator.CreateInstance<T>();
            context.CmdLineArgs = input;

            typeof(T).RunBeforeParse(context);
            var specifiedActionProperty = FindSpecifiedAction<T>(ref context.CmdLineArgs);
            context.ParserData = NewArgParser.Parse(context.CmdLineArgs);
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
                throw new ArgException("Unexpected Argument: " + context.ParserData.ImplicitParameters.First().Value);
            }

            if (context.ParserData.ExplicitParameters.Count > 0)
            {
                throw new ArgException("Unexpected Argument: " + context.ParserData.ExplicitParameters.First().Key);
            }

            return new ArgAction<T>()
            {
                Args = (T)context.Args,
                ActionArgs = specifiedActionProperty != null ? specifiedActionProperty.GetValue(context.Args, null) : null,
                ActionArgsProperty = specifiedActionProperty
            };
        }

        private PropertyInfo FindSpecifiedAction<T>(ref string[] args)
        {
            var actionProperty = ArgAction.GetActionProperty<T>();
            if (actionProperty == null) return null;

            var specifiedAction = args.Length > 0 ? args[0] : null;

            if (actionProperty.Attr<ArgRequired>().PromptIfMissing && args.Length == 0)
            {
                actionProperty.Attr<ArgRequired>().Validate(actionProperty.GetArgumentName(), ref specifiedAction);
                args = new string[] { specifiedAction };
            }

            if (specifiedAction == null) return null;

            var actionArgProperty = (from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     where p.MatchesSpecifiedAction(specifiedAction)
                                     select p).SingleOrDefault();

            if (actionArgProperty == null) throw new ArgException("Unknown Action: " + specifiedAction);

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

                if (match.Count() > 1) throw new ArgException("Argument specified more than once: "+prop.GetArgumentName());

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


            var actionProp = ArgAction.GetActionProperty(t);
            shortcuts = shortcuts ?? new List<string>();
            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (prop.IsActionArgProperty() && actionProp != null) continue;

                if (ArgRevivers.CanRevive(prop.PropertyType) == false)
                {
                    throw new InvalidArgDefinitionException("There is no reviver for type " + prop.PropertyType.Name + ". Offending Property: " + prop.DeclaringType.Name + "." + prop.GetArgumentName());
                }

                var shortcut = ArgShortcut.GetShortcut(prop);
                if (shortcut != null && shortcuts.Contains(shortcut))
                {
                    throw new InvalidArgDefinitionException("Duplicate arg options with shortcut " + shortcut);
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
