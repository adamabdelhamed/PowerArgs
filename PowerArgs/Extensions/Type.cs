using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PowerArgs
{
    internal static class TypeEx
    {
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
                var shortcuts = field.GetEnumShortcuts();
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
            if (enumType.IsEnum == false) throw new ArgumentException("Type " + enumType.Name + " is not an enum");

            List<string> shortcutsSeenSoFar = new List<string>();
            foreach (var field in enumType.GetFields().Where(f => f.IsSpecialName == false))
            {
                var shortcutsForThisField = field.GetEnumShortcuts();
                if (ignoreCase) shortcutsForThisField = shortcutsForThisField.Select(s => s.ToLower()).ToList();

                foreach (var shortcut in shortcutsForThisField)
                {
                    if (shortcutsSeenSoFar.Contains(shortcut)) throw new InvalidArgDefinitionException("Duplicate shortcuts defined for enum type '" + enumType.Name + "'");
                    shortcutsSeenSoFar.Add(shortcut);
                }
            }
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

        internal static bool TryCreate<T>(this Type t, out T val)
        {
            return TryCreate<T>(t, new object[0], out val);
        }

        internal static bool TryCreate<T>(this Type t, object[] contstructorArgs, out T val)
        {
            object ret;
            if (TryCreate(t, new Type[] { typeof(T) }, contstructorArgs, out ret))
            {
                val = (T)ret;
                return true;
            }
            else
            {
                val = default(T);
                return false;
            }
        }

        internal static bool TryCreate(this Type t, IEnumerable<Type> acceptedTypes, object[] contstructorArgs, out object val)
        {
            if (t == null)
            {
                val = null;
                return false;
            }

            foreach(var acceptableType in acceptedTypes)
            {
                if (t != acceptableType && t.GetInterfaces().Contains(acceptableType) == false && t.IsSubclassOf(acceptableType) == false)
                {
                    // no match
                }
                else
                {
                    try
                    {
                        val = Activator.CreateInstance(t, contstructorArgs);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidArgDefinitionException("Could not initialize type using required constructor: " + t.FullName, ex);
                    }
                }
            }

            if(acceptedTypes.Count() == 1)
            {
                throw new InvalidArgDefinitionException("Type does not implement " + acceptedTypes.First().Name + ": " + t.FullName);
            }
            else
            {
                var acceptableTypeList = string.Join(", ", acceptedTypes.Select(type => type.Name));
                throw new InvalidArgDefinitionException("Type does not implement any of the following types - " + acceptableTypeList + ": " + t.FullName);
            } 
        }
    }
}
