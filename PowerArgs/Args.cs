using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace PowerArgs
{
    public static class Args
    {
        public static ArgAction<T> ParseAction<T>(string[] args, ArgStyle style = ArgStyle.PowerShell)
        {
            return ParseInternal<T>(args, style);
        }

        public static T Parse<T>(string[] args, ArgStyle style = ArgStyle.PowerShell)
        {
            return ParseInternal<T>(args, style).Args;
        }

        public static ArgAction<T> InvokeAction<T>(string[] args, ArgStyle style = ArgStyle.PowerShell)
        {
            var action = Args.ParseAction<T>(args, style);
            action.Invoke();
            return action;
        }

        private static ArgAction<T> ParseInternal<T>(string[] args, ArgStyle style = ArgStyle.PowerShell)
        {
            var actionArgProperty = ResolveActionProperty<T>(ref args);
            ValidateArgScaffold<T>();

            T ret = Activator.CreateInstance<T>();
            ArgParser parser = new SmartArgParser(style, typeof(T));
            parser.Parse(args, actionArgProperty);
            PopulateProperties(ret, parser, ArgAction.GetActionProperty<T>() != null);

            if (actionArgProperty != null)
            {
                var propValue = Activator.CreateInstance(actionArgProperty.PropertyType);
                PopulateProperties(propValue, parser, false);
                actionArgProperty.SetValue(ret, propValue, null);
            }

            if (parser.Args.Keys.Count > 0)
            {
                throw new ArgException("Unexpected argument '" + parser.Args.Keys.First() + "'");
            }

            return new ArgAction<T>()
            {
                Args = ret,
                ActionArgs = actionArgProperty != null ? actionArgProperty.GetValue(ret, null) : null,
                ActionArgsProperty = actionArgProperty
            };
        }

        private static PropertyInfo ResolveActionProperty<T>(ref string[] args)
        {
            PropertyInfo actionArgProperty = null;

            var actionProperty = ArgAction.GetActionProperty<T>();

            if (actionProperty != null)
            {
                var specifiedAction = args.Length > 0 ? args[0] : null;

                if(actionProperty.Attr<ArgRequired>().PromptIfMissing && args.Length == 0)
                {
                    actionProperty.Attr<ArgRequired>().Validate(actionProperty.Name, ref specifiedAction);
                    if (specifiedAction != null)
                    {
                        args = new string[] { specifiedAction };
                    }
                }

                if (specifiedAction != null)
                {
                    actionArgProperty = (from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                         where p.Name.ToLower() == specifiedAction.ToLower() + "args"
                                         select p).SingleOrDefault();

                    if (actionArgProperty == null)
                    {
                        throw new ArgException("Unknown Action: " + specifiedAction);
                    }
                }
              
            }

            return actionArgProperty;
        }

        private static void PopulateProperties(object toPopulate , ArgParser parser, bool ignoreActionProperties)
        {
            foreach (PropertyInfo prop in toPopulate.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (prop.Name.EndsWith("Args") && ignoreActionProperties) continue;

                string argName = prop.Name.ToLower();
                string argValue = parser.Args.ContainsKey(argName) ? parser.Args[argName] : null;
                var argShortcut = ArgShortcut.GetShortcut(prop);

                if (argValue == null && argShortcut != null) // then see if the shortcut was specified
                {
                    argValue = parser.Args.ContainsKey(argShortcut) ? parser.Args[argShortcut] : null;
                }

                if (argValue == null && prop.Attr<StickyArg>() != null)
                {
                    argValue = prop.Attr<StickyArg>().GetStickyArg(argName);
                }

                try
                {
                    if (prop.HasAttr<ArgRequired>()) prop.Attr<ArgRequired>().Validate(argName, ref argValue);

                    if (argValue != null)
                    {
                        foreach (var v in prop.Attrs<ArgValidator>()) v.Validate(argName, ref argValue);
                    }
                }
                catch (ArgException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ArgException(ex.Message, ex);
                }

                var defaultValueAttr = prop.Attr<DefaultValueAttribute>();
                if (argValue == null && defaultValueAttr != null)
                {
                    prop.SetValue(toPopulate, defaultValueAttr.Value, null);
                }
                else if (prop.PropertyType.IsEnum && defaultValueAttr != null)
                {
                    prop.SetValue(toPopulate, defaultValueAttr.Value, null);
                }

                if (prop.PropertyType.IsEnum && argValue != null)
                {
                    try
                    {
                        prop.SetValue(toPopulate, Enum.Parse(prop.PropertyType, argValue), null);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgException("'" + argValue + "' is not a valid value for " + prop.Name + ". Available values are [" + string.Join(", ", Enum.GetNames(prop.PropertyType)) + "]", ex);
                    }
                }
                else if (ArgRevivers.Revivers.ContainsKey(prop.PropertyType) && argValue != null)
                {
                    try
                    {
                        prop.SetValue(toPopulate, ArgRevivers.Revivers[prop.PropertyType](argName, argValue), null);
                    }
                    catch (ArgException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new ArgException(ex.Message, ex);
                    }
                }
                else if(argValue != null)  
                {
                    throw new ArgException("Unexpected argument '" + argName + "' with value '" + argValue + "'");
                }


                if (argValue != null && prop.HasAttr<StickyArg>()) prop.Attr<StickyArg>().SetStickyArg(argName, argValue);

                parser.Args.Remove(argName);
                if(argShortcut != null) parser.Args.Remove(argShortcut);
            }
        }

        private static void ValidateArgScaffold<T>()
        {
            ValidateArgScaffold(typeof(T));
        }

        private static void ValidateArgScaffold(Type t, List<string> shortcuts = null)
        {
            var actionProp = ArgAction.GetActionProperty(t);
            shortcuts = shortcuts ?? new List<string>();
            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (prop.Name.EndsWith("Args") && actionProp != null) continue;

                if (prop.PropertyType.IsEnum == false &&  ArgRevivers.Revivers.ContainsKey(prop.PropertyType) == false)
                {
                    ArgRevivers.SearchAssemblyForRevivers(prop.PropertyType.Assembly);
                    if (ArgRevivers.Revivers.ContainsKey(prop.PropertyType) == false)
                    {
                        throw new InvalidArgDefinitionException("There is no reviver for type " + prop.PropertyType.Name + ". Offending Property: " + prop.DeclaringType.Name + "." + prop.Name);
                    }
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
                    if (prop.Name.EndsWith("Args"))
                    {
                        ArgAction.ResolveMethod(t,prop);
                        ValidateArgScaffold(prop.PropertyType, shortcuts.ToArray().ToList());
                    }
                }
            }
        }
    }
}
