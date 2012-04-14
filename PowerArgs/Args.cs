using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace PowerArgs
{
    public class Args
    {
        private ArgOptions Options { get; set; }
        private Args() { }

        public static ArgAction<T> ParseAction<T>(string[] args, ArgStyle style = ArgStyle.PowerShell)
        {
            Args instance = new Args() { Options = ArgOptions.DefaultOptions };
            var options = ArgOptions.DefaultOptions;
            options.Style = style;
            return instance.ParseInternal<T>(args, options);
        }

        public static T Parse<T>(string[] args, ArgStyle style = ArgStyle.PowerShell)
        {
            return ParseAction<T>(args, style).Args;
        }

        public static ArgAction<T> InvokeAction<T>(string[] args, ArgStyle style = ArgStyle.PowerShell)
        {
            var action = Args.ParseAction<T>(args, style);
            action.Invoke();
            return action;
        }

        private ArgAction<T> ParseInternal<T>(string[] args, ArgOptions options)
        {
            ValidateArgScaffold<T>();

            var context = new ArgHook.HookContext();
            context.Args = Activator.CreateInstance<T>();
            context.Parser = new SmartArgParser(options, typeof(T));
            var specifiedActionProperty = FindSpecifiedAction<T>(ref args);

            context.Parser.Parse(args, specifiedActionProperty);

            typeof(T).RunBeforePopulateProperties(context);

            PopulateProperties(context.Args, context.Parser, specifiedActionProperty != null);

            if (specifiedActionProperty != null)
            {
                var actionPropertyValue = Activator.CreateInstance(specifiedActionProperty.PropertyType);
                PopulateProperties(actionPropertyValue, context.Parser, false);
                specifiedActionProperty.SetValue(context.Args, actionPropertyValue, null);
            }

            typeof(T).RunAfterPopulateProperties(context);

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
                actionProperty.Attr<ArgRequired>().Validate(actionProperty.GetArgumentName(Options), ref specifiedAction);
                args = new string[] { specifiedAction };
            }

            if (specifiedAction == null) return null;

            var actionArgProperty = (from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     where p.MatchesSpecifiedAction(specifiedAction, Options)
                                     select p).SingleOrDefault();

            if (actionArgProperty == null) throw new ArgException("Unknown Action: " + specifiedAction);

            return actionArgProperty;
        }

        private void PopulateProperties(object toPopulate , ArgParser parser, bool ignoreActionProperties)
        {
            foreach (PropertyInfo prop in toPopulate.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (prop.IsActionProperty() && ignoreActionProperties) continue;

                var context = new ArgHook.HookContext()
                {
                    ArgumentValue = parser.Args.ContainsKey(prop.GetArgumentName(Options)) ? parser.Args[prop.GetArgumentName(Options)] : null,
                    Parser = parser,
                    Property = prop,
                    RevivedProperty = null,
                    Options = Options,
                    Args = toPopulate
                };

                prop.RunBeforePopulateProperty(context);
                prop.Validate(context);
                prop.Revive(toPopulate, context);
                prop.RunAfterPopulateProperty(context);
            }
        }

        private void ValidateArgScaffold<T>()
        {
            ValidateArgScaffold(typeof(T));
        }

        private void ValidateArgScaffold(Type t, List<string> shortcuts = null)
        {
            var actionProp = ArgAction.GetActionProperty(t);
            shortcuts = shortcuts ?? new List<string>();
            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (prop.IsActionProperty() && actionProp != null) continue;

                if (ArgRevivers.CanRevive(prop.PropertyType) == false)
                {
                    throw new InvalidArgDefinitionException("There is no reviver for type " + prop.PropertyType.Name + ". Offending Property: " + prop.DeclaringType.Name + "." + prop.GetArgumentName(Options));
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
                    if (prop.IsActionProperty())
                    {
                        ArgAction.ResolveMethod(t,prop);
                        ValidateArgScaffold(prop.PropertyType, shortcuts.ToArray().ToList());
                    }
                }
            }
        }
    }
}
