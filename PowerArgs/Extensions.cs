using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace PowerArgs
{
    public static class Extensions
    {
        public static string GetArgumentName(this PropertyInfo prop, ArgOptions options)
        {
            if (options.IgnoreCaseForPropertyNames) return prop.Name.ToLower();
            else return prop.Name;
        }

        public static bool MatchesSpecifiedAction(this PropertyInfo prop, string action, ArgOptions options)
        {
            var propName = prop.GetArgumentName(options);
            var test = options.IgnoreCaseForPropertyNames ? action.ToLower() + ArgSettings.ActionArgConventionSuffix.ToLower() : action + ArgSettings.ActionArgConventionSuffix;
            return test == propName;
        }

        public static bool IsActionProperty(this PropertyInfo prop)
        {
            return prop.Name.EndsWith(ArgSettings.ActionArgConventionSuffix);
        }

        public static void Validate(this PropertyInfo prop, ArgHook.HookContext context)
        {
            if (prop.HasAttr<ArgRequired>())
            {
                prop.Attr<ArgRequired>().Validate(prop.GetArgumentName(context.Options), ref context.ArgumentValue);
            }

            if (context.ArgumentValue != null)
            {
                foreach (var v in prop.Attrs<ArgValidator>().OrderByDescending(val => val.Priority))
                {
                    v.Validate(prop.GetArgumentName(context.Options), ref context.ArgumentValue);
                }
            }
        }

        public static void Revive(this PropertyInfo prop, object toRevive, ArgHook.HookContext context)
        {
            if (ArgRevivers.CanRevive(prop.PropertyType) && context.ArgumentValue != null)
            {
                try
                {
                    context.RevivedProperty = ArgRevivers.Revive(prop.PropertyType, prop.GetArgumentName(context.Options), context.ArgumentValue);
                    prop.SetValue(toRevive, context.RevivedProperty, null);
                }
                catch (ArgException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (prop.PropertyType.IsEnum) throw new ArgException("'" + context.ArgumentValue + "' is not a valid value for " + prop.GetArgumentName(context.Options) + ". Available values are [" + string.Join(", ", Enum.GetNames(prop.PropertyType)) + "]", ex);
                    else throw new ArgException(ex.Message, ex);
                }
            }
            else if (context.ArgumentValue != null)
            {
                throw new ArgException("Unexpected argument '" + prop.GetArgumentName(context.Options) + "' with value '" + context.ArgumentValue + "'");
            }
        }

        public static void RunBeforePopulateProperties(this Type t, ArgHook.HookContext context)
        {
            foreach (var hook in t.GetHooks(h => h.BeforePopulatePropertiesPriority))
            {
                hook.BeforePopulateProperties(context);
            }
        }

        public static void RunAfterPopulateProperties(this Type t, ArgHook.HookContext context)
        {
            foreach (var hook in t.GetHooks(h => h.AfterPopulatePropertiesPriority))
            {
                hook.AfterPopulateProperties(context);
            }
        }

        public static void RunBeforePopulateProperty(this PropertyInfo prop, ArgHook.HookContext context)
        {
            foreach (var hook in prop.GetHooks(h => h.BeforePopulatePropertyPriority))
            {
                hook.BeforePopulateProperty(context);
            }
        }

        public static void RunAfterPopulateProperty(this PropertyInfo prop, ArgHook.HookContext context)
        {
            foreach (var hook in prop.GetHooks(h => h.AfterPopulatePropertyPriority))
            {
                hook.AfterPopulateProperty(context);
                context.Property.SetValue(context.Args, context.RevivedProperty, null);
            }
        }

        public static List<ArgHook> GetHooks(this PropertyInfo prop, Func<ArgHook,int> priority)
        {
            var hooks = prop.Attrs<ArgHook>();
            
            if (prop.GetHook<ArgShortcut>() == null)  hooks.Add(new ArgShortcut(ArgShortcut.GetShortcut(prop)));
            if (prop.GetHook<ParserCleanupHook>() == null) hooks.Add(new ParserCleanupHook());
            
            hooks = hooks.OrderByDescending(priority).ToList();
            return hooks;
        }

        public static List<ArgHook> GetHooks(this Type t, Func<ArgHook, int> priority)
        {
            var hooks = t.Attrs<ArgHook>();
            if (t.GetHook<ParserCleanupHook>() == null) hooks.Add(new ParserCleanupHook());
            hooks = hooks.OrderByDescending(priority).ToList();
            return hooks;
        }

        public static T GetHook<T>(this MemberInfo prop) where T : ArgHook
        {
            return (T)(from h in prop.Attrs<ArgHook>()
                    where h.GetType() == typeof(T)
                    select h).FirstOrDefault();
        }

        public static bool HasAttr<T>(this MemberInfo info)
        {
            return info.GetCustomAttributes(typeof(T), true).Length > 0;
        }


        public static T Attr<T>(this MemberInfo info)
        {
            if (info.HasAttr<T>())
            {
                return (T)info.GetCustomAttributes(typeof(T), true)[0];
            }
            else
            {
                return default(T);
            }
        }


        public static List<T> Attrs<T>(this MemberInfo info)
        {
            if (info.HasAttr<T>())
            {
                return (from attr in info.GetCustomAttributes(typeof(T), true) select (T)attr).ToList();
            }
            else
            {
                return new List<T>();
            }
        }
    }
}
