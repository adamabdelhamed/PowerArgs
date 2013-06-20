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
        internal static bool IgnoreCase(this PropertyInfo prop)
        {
            bool ignoreCase = true;

            if (prop.HasAttr<ArgIgnoreCase>() && prop.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ignoreCase = false;
            }
            else if (prop.DeclaringType.HasAttr<ArgIgnoreCase>() && prop.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ignoreCase = false;
            }

            return ignoreCase;
        }

        internal static List<PropertyInfo> FindAllEnumArguments(this Type t)
        {
            return t.FindAllArguments().Where(p => p.PropertyType.IsEnum).ToList();
        }

        internal static List<PropertyInfo> FindAllArguments(this Type t)
        {
            List<PropertyInfo> ret = t.GetArguments();
 
            foreach (var action in t.GetActionArgProperties())
            {
                ret.AddRange(action.PropertyType.GetArguments());
            }

            return ret;
        }

        internal static List<string> GetEnumShortcuts(this FieldInfo enumField)
        {
            if (enumField.DeclaringType.IsEnum == false) throw new ArgumentException("The given field '"+enumField.Name+"' is not an enum field.");

            var shortcutAttrs = enumField.Attrs<ArgShortcut>();
            var noShortcutPolicy = shortcutAttrs.Where(s => s.Shortcut == null).SingleOrDefault();
            var shortcutVals = shortcutAttrs.Where(s => s.Shortcut != null).Select(s => s.Shortcut).ToList();

            if (noShortcutPolicy != null && shortcutVals.Count > 0) throw new InvalidArgDefinitionException("You can't have an ArgShortcut attribute with a null shortcut and then define a second ArgShortcut attribute with a non-null value.");

            return shortcutVals;
        }

        internal static List<string> GetEnumShortcuts(this Type enumType)
        {
            List<string> ret = new List<string>();
            foreach (var field in enumType.GetFields().Where(f => f.IsSpecialName == false))
            {
                ret.AddRange(field.GetEnumShortcuts());
            }
            return ret;
        }

        internal static bool TryMatchEnumShortcut(this Type enumType, string value, bool ignoreCase, out object enumResult)
        {
            if (ignoreCase) value = value.ToLower();
            foreach (var field in enumType.GetFields().Where(f => f.IsSpecialName == false))
            {
                var shortcuts = GetEnumShortcuts(field);
                if (ignoreCase) shortcuts = shortcuts.Select(s => s.ToLower()).ToList();
                var match = (from s in shortcuts where s == value select s).SingleOrDefault();
                if (match != null)
                {
                    enumResult = Enum.Parse(enumType, field.Name);
                    return true;
                }
            }

            enumResult = null;
            return false;
        }

        internal static void ValidateNoDuplicateEnumShortcuts(this Type enumType, bool ignoreCase)
        {
            if (enumType.IsEnum == false) throw new ArgumentException("Type "+enumType.Name+" is not an enum");

            List<string> shortcutsSeenSoFar = new List<string>();
            foreach (var field in enumType.GetFields().Where(f => f.IsSpecialName == false))
            {
                var shortcutsForThisField = GetEnumShortcuts(field);
                if (ignoreCase) shortcutsForThisField = shortcutsForThisField.Select(s => s.ToLower()).ToList();

                foreach (var shortcut in shortcutsForThisField)
                {
                    if (shortcutsSeenSoFar.Contains(shortcut)) throw new InvalidArgDefinitionException("Duplicate shortcuts defined for enum type '"+enumType.Name+"'");
                    shortcutsSeenSoFar.Add(shortcut);
                }
            }
        }

        internal static void ValidateNoConflictingShortcutPolicies(this PropertyInfo property)
        {
            var attrs = property.Attrs<ArgShortcut>();
            var noShortcutsAllowed = attrs.Where(a => a.Policy == ArgShortcutPolicy.NoShortcut).Count() != 0;
            var shortcutsOnly = attrs.Where(a => a.Policy == ArgShortcutPolicy.ShortcutsOnly).Count() != 0;
            var actualShortcutValues = attrs.Where(a => a.Policy.HasValue == false && a.Shortcut != null).Count() != 0;

            if (noShortcutsAllowed && shortcutsOnly) throw new InvalidArgDefinitionException("You cannot specify a policy of NoShortcut and another policy of ShortcutsOnly.");
            if (noShortcutsAllowed && actualShortcutValues) throw new InvalidArgDefinitionException("You cannot specify a policy of NoShortcut and then also specify shortcut values via another attribute.");
            if (shortcutsOnly && actualShortcutValues == false) throw new InvalidArgDefinitionException("You specified a policy of ShortcutsOnly, but did not specify any shortcuts by adding another ArgShortcut attrivute.");
        }

        internal static MethodInfo InvokeMainMethod(this object o)
        {
            var method = o.GetType().GetMethod("Main");
            if (method == null) throw new InvalidArgDefinitionException("There is no Main() method in type "+o.GetType().Name);
            if (method.IsStatic) throw new InvalidArgDefinitionException("The Main() method in type '" + o.GetType().Name+"' must not be static");
            if (method.GetParameters().Length > 0) throw new InvalidArgDefinitionException("The Main() method in type '" + o.GetType().Name + "' must not take any parameters");
            if (method.ReturnType != null && method.ReturnType != typeof(void)) throw new InvalidArgDefinitionException("The Main() method in type '" + o.GetType().Name + "' must return void");

            method.Invoke(o, new object[0]);

            return method;
        }

        internal static List<PropertyInfo> GetArguments(this Type t)
        {
            return (from  prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    where prop.Attr<ArgIgnoreAttribute>() == null &&
                          prop.IsActionArgProperty() == false
                    select prop).ToList();
        }

        internal static List<PropertyInfo> GetActionArgProperties(this Type t)
        {
            List<PropertyInfo> ret = new List<PropertyInfo>();

            string[] dummy = new string[0];
            if (t.FindSpecifiedAction(ref dummy, false) == null && t.GetActionMethods().Count == 0)
            {
                return ret;
            }

            ret.AddRange(from p in t.GetProperties() where p.IsActionArgProperty() select p);
            ret.AddRange(from m in t.GetActionMethods() select new ArgActionMethodVirtualProperty(m));
            return ret;
        }


        internal static PropertyInfo FindSpecifiedAction(this Type t, ref string[] args, bool enablePrompts = true)
        {
            var actionProperty = ArgAction.GetActionProperty(t);

            if (actionProperty == null && t.GetActionMethods().Count == 0) return null;

            var specifiedAction = args.Length > 0 ? args[0] : null;

            if (enablePrompts && actionProperty != null && actionProperty.Attr<ArgRequired>().PromptIfMissing && args.Length == 0)
            {
                actionProperty.Attr<ArgRequired>().ValidateAlways(actionProperty, ref specifiedAction);
                args = new string[] { specifiedAction };
            }
            else if (enablePrompts && actionProperty == null && specifiedAction == null && t.GetActionMethods().Count > 0)
            {
                new ArgRequired().ValidateAlways(new VirtualNamedProperty("Action", typeof(string), t), ref specifiedAction);
            }

            if (specifiedAction == null) return null;

            var actionArgProperty = (from p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     where p.MatchesSpecifiedAction(specifiedAction)
                                     select p).SingleOrDefault();

            if (actionArgProperty == null)
            {
                var matchingActionMethod = t.GetActionMethods().Where(m => m.MatchesSpecifiedAction(specifiedAction)).SingleOrDefault();

                if (matchingActionMethod == null)
                {
                    throw new UnknownActionArgException("Unknown Action: " + specifiedAction);
                }

                return new ArgActionMethodVirtualProperty(matchingActionMethod);
            }

            return actionArgProperty;
        }

        internal static List<MethodInfo> GetActionMethods(this Type t)
        {
            if (t.HasAttr<ArgActionType>())
            {
                t = t.Attr<ArgActionType>().ActionType;
            }

            return (from m in t.GetMethods()
                    where m.HasAttr<ArgActionMethod>()
                    select m).ToList();
        }

        internal static string GetArgumentName(this PropertyInfo prop)
        {
            bool ignoreCase = prop.IgnoreCase();

            if (ignoreCase) return prop.Name.ToLower();
            else return prop.Name;
        }

        internal static bool MatchesSpecifiedArg(this PropertyInfo prop, string specifiedArg)
        {
            if (prop.HasAttr<ArgIgnoreAttribute>()) return false;

            bool ignoreCase = prop.IgnoreCase();

            bool ignorePropertyName = prop.Attrs<ArgShortcut>().Where(s => s.Policy == ArgShortcutPolicy.ShortcutsOnly).Count() > 0;

            var shortcuts = ArgShortcut.GetShortcutsInternal(prop);

            if (ignoreCase && shortcuts.Count > 0)
            {
                return shortcuts.Where(shortcut => (!ignorePropertyName && prop.Name.ToLower() == specifiedArg.ToLower()) || shortcut.ToLower() == specifiedArg.ToLower()).Count() > 0;
            }
            else if(ignoreCase)
            {
                return (!ignorePropertyName && prop.Name.ToLower() == specifiedArg.ToLower());
            }
            else
            {
                return (!ignorePropertyName && prop.Name == specifiedArg) || shortcuts.Where(shortcut => shortcut == specifiedArg).Count() > 0;
            }
        }

        internal static bool MatchesSpecifiedAction(this PropertyInfo prop, string action)
        {
            var propName = prop.GetArgumentName();
            var test = (prop.HasAttr<ArgIgnoreCase>() && !prop.Attr<ArgIgnoreCase>().IgnoreCase) ||
                (prop.DeclaringType.HasAttr<ArgIgnoreCase>() && !prop.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase) ? action + Constants.ActionArgConventionSuffix : action.ToLower() + Constants.ActionArgConventionSuffix.ToLower();
            return test == propName;
        }

        internal static bool MatchesSpecifiedAction(this MethodInfo method, string action)
        {
            var methodName = method.Name;

            bool ignoreCase = true;

            if (method.HasAttr<ArgIgnoreCase>() && !method.Attr<ArgIgnoreCase>().IgnoreCase)
            {
                ignoreCase = false;
            }
            else if (method.DeclaringType.HasAttr<ArgIgnoreCase>() && !method.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase)
            {
                ignoreCase = false;
            }

            if (ignoreCase)
            {
                action = action.ToLower();
                methodName = methodName.ToLower();
            }

            return action == methodName;
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
                        bool ignoreCase = prop.IgnoreCase();

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

            foreach (var arg in t.GetArguments())
            {
                foreach (var hook in arg.GetHooks(h => h.BeforeParsePriority))
                {
                    context.Property = arg;
                    hook.BeforeParse(context);
                    context.Property = null;
                }
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


        internal static List<UsageHook> GetUsageHooks(this PropertyInfo member)
        {
            var hooks = member.Attrs<UsageHook>();
            if (ArgUsage.ExplicitPropertyHooks.ContainsKey(member))
            {
                hooks.AddRange(ArgUsage.ExplicitPropertyHooks[member]);
            }

            hooks.AddRange(ArgUsage.GlobalUsageHooks);

            hooks.AddRange(member.DeclaringType.Attrs<UsageHook>());
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
            return info.Attrs<T>().Count > 0;
        }


        internal static T Attr<T>(this MemberInfo info)
        {
            if (info.HasAttr<T>())
            {
                return info.Attrs<T>()[0];
            }
            else
            {
                return default(T);
            }
        }


        internal static List<T> Attrs<T>(this MemberInfo info)
        {
            return (from attr in info.GetCustomAttributes(true) where attr.GetType() == typeof(T) || attr.GetType().IsSubclassOf(typeof(T)) select (T)attr).ToList();
        }

        internal static List<Match> ToList(this MatchCollection matches)
        {
            List<Match> ret = new List<Match>();
            foreach (Match match in matches) ret.Add(match);
            return ret;
        }
    }
}
