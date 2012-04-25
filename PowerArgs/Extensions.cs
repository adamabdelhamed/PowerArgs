using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace PowerArgs
{
    internal static class Extensions
    {
        internal static ArgStyle GetArgStyle(this Type argType)
        {
            return argType.HasAttr<ArgStyleAttribute>() ? argType.Attr<ArgStyleAttribute>().Style : default(ArgStyle);
        }

        internal static string GetArgumentName(this PropertyInfo prop)
        {
            bool ignoreCase = true;

            if (prop.HasAttr<ArgIgnoreCase>() && !prop.Attr<ArgIgnoreCase>().IgnoreCase) ignoreCase = false;
            else if (prop.DeclaringType.HasAttr<ArgIgnoreCase>() && !prop.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase) ignoreCase = false;

            if (ignoreCase) return prop.Name.ToLower();
            else return prop.Name;
        }

        internal static bool MatchesSpecifiedArg(this PropertyInfo prop, string specifiedArg)
        {
            bool ignoreCase = true;

            if (prop.HasAttr<ArgIgnoreCase>() && !prop.Attr<ArgIgnoreCase>().IgnoreCase) ignoreCase = false;
            else if (prop.DeclaringType.HasAttr<ArgIgnoreCase>() && !prop.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase) ignoreCase = false;

            var shortcut = ArgShortcut.GetShortcut(prop);

            if (ignoreCase && shortcut != null)
            {
                return prop.Name.ToLower() == specifiedArg.ToLower() || shortcut.ToLower() == specifiedArg.ToLower();
            }
            else if(ignoreCase)
            {
                return prop.Name.ToLower() == specifiedArg.ToLower();
            }
            else
            {
                return prop.Name == specifiedArg || shortcut == specifiedArg;
            }
        }

        internal static bool MatchesSpecifiedAction(this PropertyInfo prop, string action)
        {
            var propName = prop.GetArgumentName();
            var test = (prop.HasAttr<ArgIgnoreCase>() && !prop.Attr<ArgIgnoreCase>().IgnoreCase) ||
                (prop.DeclaringType.HasAttr<ArgIgnoreCase>() && !prop.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase) ? action + Constants.ActionArgConventionSuffix : action.ToLower() + Constants.ActionArgConventionSuffix.ToLower();
            return test == propName;
        }

        internal static bool IsActionArgProperty(this PropertyInfo prop)
        {
            return prop.Name.EndsWith(Constants.ActionArgConventionSuffix);
        }

        internal static void Validate(this PropertyInfo prop, ArgHook.HookContext context)
        {
            if (prop.HasAttr<ArgRequired>())
            {
                prop.Attr<ArgRequired>().Validate(prop.GetArgumentName(), ref context.ArgumentValue);
            }

            if (context.ArgumentValue != null)
            {
                foreach (var v in prop.Attrs<ArgValidator>().OrderByDescending(val => val.Priority))
                {
                    v.Validate(prop.GetArgumentName(), ref context.ArgumentValue);
                }
            }
        }

        internal static void Revive(this PropertyInfo prop, object toRevive, ArgHook.HookContext context)
        {
            if (ArgRevivers.CanRevive(prop.PropertyType) && context.ArgumentValue != null)
            {
                try
                {
                    context.RevivedProperty = ArgRevivers.Revive(prop.PropertyType, prop.GetArgumentName(), context.ArgumentValue);
                    prop.SetValue(toRevive, context.RevivedProperty, null);
                }
                catch (ArgException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (prop.PropertyType.IsEnum) throw new ArgException("'" + context.ArgumentValue + "' is not a valid value for " + prop.GetArgumentName() + ". Available values are [" + string.Join(", ", Enum.GetNames(prop.PropertyType)) + "]", ex);
                    else throw new ArgException(ex.Message, ex);
                }
            }
            else if (context.ArgumentValue != null)
            {
                throw new ArgException("Unexpected argument '" + prop.GetArgumentName() + "' with value '" + context.ArgumentValue + "'");
            }
        }

        internal static void RunBeforeParse(this Type t, ArgHook.HookContext context)
        {
            foreach (var hook in t.GetHooks(h => h.BeforeParsePriority))
            {
                hook.BeforeParse(context);
            }
        }

        internal static void RunBeforePopulateProperties(this Type t, ArgHook.HookContext context)
        {
            foreach (var hook in t.GetHooks(h => h.BeforePopulatePropertiesPriority))
            {
                hook.BeforePopulateProperties(context);
            }

            var toRestore = context.Property;
            foreach (PropertyInfo prop in t.GetProperties())
            {
                context.Property = prop;
                foreach (var hook in prop.GetHooks(h => h.BeforePopulatePropertiesPriority))
                {
                    hook.BeforePopulateProperties(context);
                }
            }
            context.Property = toRestore;
        }

        internal static void RunAfterPopulateProperties(this Type t, ArgHook.HookContext context)
        {
            foreach (var hook in t.GetHooks(h => h.AfterPopulatePropertiesPriority))
            {
                hook.AfterPopulateProperties(context);
            }

            var toRestore= context.Property;
            foreach (PropertyInfo prop in t.GetProperties())
            {
                context.Property = prop;
                foreach (var hook in prop.GetHooks(h => h.AfterPopulatePropertiesPriority))
                {
                    hook.AfterPopulateProperties(context);
                }
            }
            context.Property = toRestore;
        }

        internal static void RunBeforePopulateProperty(this PropertyInfo prop, ArgHook.HookContext context)
        {
            foreach (var hook in prop.GetHooks(h => h.BeforePopulatePropertyPriority))
            {
                hook.BeforePopulateProperty(context);
            }
        }

        internal static void RunAfterPopulateProperty(this PropertyInfo prop, ArgHook.HookContext context)
        {
            foreach (var hook in prop.GetHooks(h => h.AfterPopulatePropertyPriority))
            {
                hook.AfterPopulateProperty(context);
                context.Property.SetValue(context.Args, context.RevivedProperty, null);
            }
        }

        internal static List<ArgHook> GetHooks(this MemberInfo member, Func<ArgHook,int> priority)
        {
            var hooks = member.Attrs<ArgHook>();
            hooks = hooks.OrderByDescending(priority).ToList();
            return hooks;
        }

        internal static T GetHook<T>(this MemberInfo prop) where T : ArgHook
        {
            return (T)(from h in prop.Attrs<ArgHook>()
                    where h.GetType() == typeof(T)
                    select h).FirstOrDefault();
        }

        internal static bool HasAttr<T>(this MemberInfo info)
        {
            return info.GetCustomAttributes(typeof(T), true).Length > 0;
        }


        internal static T Attr<T>(this MemberInfo info)
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


        internal static List<T> Attrs<T>(this MemberInfo info)
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
