using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    internal static class Extensions
    {
        internal static List<PropertyInfo> GetArguments(this Type t)
        {
            return (from  prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    where prop.Attr<ArgIgnoreAttribute>() == null &&
                          prop.IsActionArgProperty() == false
                    select prop).ToList();
        }

        internal static List<PropertyInfo> GetActionArgProperties(this Type t)
        {
            if (ArgAction.GetActionProperty(t) == null) return new List<PropertyInfo>();

            return (from prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    where prop.IsActionArgProperty() select prop).ToList();
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
            if (prop.PropertyType == typeof(SecureStringArgument) && prop.Attrs<ArgValidator>().Count > 0)
            {
                throw new InvalidArgDefinitionException("Properties of type SecureStringArgument cannot be validated.  If your goal is to make the argument required then the[ArgRequired] attribute is not needed.  The SecureStringArgument is designed to prompt the user for a value only if your code asks for it after parsing.  If your code never reads the SecureString property then the user is never prompted and it will be treated as an optional parameter.  Although discouraged, if you really, really need to run custom logic against the value before the rest of your program runs then you can implement a custom ArgHook, override RunAfterPopulateProperty, and add your custom attribute to the SecureStringArgument property.");
            }

            foreach (var v in prop.Attrs<ArgValidator>().OrderByDescending(val => val.Priority))
            {
                if (v.ImplementsValidateAlways)
                {
                    v.ValidateAlways(prop, ref context.ArgumentValue);
                }
                else if (context.ArgumentValue != null)
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
                    if (prop.PropertyType.IsEnum)
                    {
                        bool ignoreCase = true;

                        if (prop.HasAttr<ArgIgnoreCase>() && prop.Attr<ArgIgnoreCase>().IgnoreCase == false)
                        {
                            ignoreCase = true;
                        }

                        context.RevivedProperty = ArgRevivers.ReviveEnum(prop.PropertyType, context.ArgumentValue, ignoreCase );
                    }
                    else
                    {
                        context.RevivedProperty = ArgRevivers.Revive(prop.PropertyType, prop.GetArgumentName(), context.ArgumentValue);
                    }
                    prop.SetValue(toRevive, context.RevivedProperty, null);
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
                        if (prop.PropertyType.IsEnum) throw new ArgException("'" + context.ArgumentValue + "' is not a valid value for " + prop.GetArgumentName() + ". Available values are [" + string.Join(", ", Enum.GetNames(prop.PropertyType)) + "]", ex);
                        else throw new ArgException(ex.Message, ex);
                    }
                }
            }
            else if (ArgRevivers.CanRevive(prop.PropertyType) && prop.PropertyType == typeof(SecureStringArgument))
            {
                context.RevivedProperty = ArgRevivers.Revive(prop.PropertyType, prop.GetArgumentName(), context.ArgumentValue);
                prop.SetValue(toRevive, context.RevivedProperty, null);
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

        internal static List<Match> ToList(this MatchCollection matches)
        {
            List<Match> ret = new List<Match>();
            foreach (Match match in matches) ret.Add(match);
            return ret;
        }
    }
}
