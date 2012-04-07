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
            var actionArgProperty = ResolveActionProperty<T>(args);
            ValidateArgScaffold<T>(GetActionProperty<T>() != null);

            T ret = Activator.CreateInstance<T>();
            ArgParser parser = new SmartArgParser(style, typeof(T));
            parser.Parse(args, actionArgProperty);
            PopulateProperties(ret, parser, GetActionProperty<T>() != null);

            if (actionArgProperty != null)
            {
                var propValue = Activator.CreateInstance(actionArgProperty.PropertyType);
                PopulateProperties(propValue, parser, false);
                actionArgProperty.SetValue(ret, propValue, null);
            }

            return new ArgAction<T>()
            {
                Args = ret,
                ActionArgs = actionArgProperty != null ? actionArgProperty.GetValue(ret, null) : null,
                ActionArgsProperty = actionArgProperty
            };
        }

        private static PropertyInfo ResolveActionProperty<T>(string[] args)
        {
            PropertyInfo actionArgProperty = null;

            var actionProperty = GetActionProperty<T>();

            if (actionProperty != null)
            {
                var specifiedAction = args.Length > 0 ? args[0] : null;
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
                else
                {
                    throw new ArgException("The action argument is required");
                }
            }

            return actionArgProperty;
        }

        public static PropertyInfo GetActionProperty<T>()
        {
            var actionProperty = (from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                  where p.Name == "Action" &&
                                        p.Attr<ArgPosition>() != null && p.Attr<ArgPosition>().Position == 0 &&
                                        p.HasAttr<ArgRequired>()
                                  select p).SingleOrDefault();
            return actionProperty;
        }

        private static void PopulateProperties(object toPopulate , ArgParser parser, bool ignoreActionProperties)
        {
            foreach (PropertyInfo prop in toPopulate.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (prop.Name.EndsWith("Args") && ignoreActionProperties) continue;

                string argName = prop.Name.ToLower();
                string argValue = parser.Args.ContainsKey(argName) ? parser.Args[argName] : null;
                string argShortcut = prop.Name.ToLower()[0] + "";

                if (argValue == null) // then see if the shortcut was specified
                {
                    argValue = parser.Args.ContainsKey(argShortcut) ? parser.Args[argShortcut] : null;
                    if (argValue != null) argName = argShortcut;
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
                    prop.SetValue(toPopulate, ArgRevivers.Revivers[prop.PropertyType](argName, argValue), null);
                }
                else if(argValue != null)  
                {
                    throw new ArgException("Unexpected argument '" + argName + "' with value '" + argValue + "'");
                }
            }
        }

        private static void ValidateArgScaffold<T>(bool hasActionProperties)
        {
            List<char> shortcuts = new List<char>();
            foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (prop.Name.EndsWith("Args") && hasActionProperties) continue;

                if (prop.PropertyType.IsEnum == false &&  ArgRevivers.Revivers.ContainsKey(prop.PropertyType) == false)
                {
                    ArgRevivers.SearchAssemblyForRevivers(prop.PropertyType.Assembly);
                    if (ArgRevivers.Revivers.ContainsKey(prop.PropertyType) == false)
                    {
                        throw new InvalidArgDefinitionException("There is no reviver for type " + prop.PropertyType.Name + ". Offending Property: " + prop.DeclaringType.Name + "." + prop.Name);
                    }
                }

                char shortcut = prop.Name.ToLower()[0];
                if (shortcuts.Contains(shortcut))
                {
                    throw new InvalidArgDefinitionException("Duplicate arg options with shortcut " + prop.Name.ToLower()[0]);
                }
                else
                {
                    shortcuts.Add(shortcut);
                }
            }

            if (hasActionProperties)
            {
                foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (prop.Name.EndsWith("Args"))
                    {
                        ArgAction<T>.ResolveMethod(prop);
                    }
                }
            }
        }
    }
}
