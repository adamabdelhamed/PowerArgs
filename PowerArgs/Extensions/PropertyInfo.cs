using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PowerArgs
{
    internal static class PropertyInfoEx
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

        internal static string GetArgumentName(this PropertyInfo prop)
        {
            bool ignoreCase = prop.IgnoreCase();


            if (ignoreCase) return prop.Name.ToLower();
            else return prop.Name;
        }
 

        internal static void ValidateNoConflictingShortcutPolicies(this PropertyInfo property)
        {
            var attrs = property.Attrs<ArgShortcut>();
            var noShortcutsAllowed = attrs.Where(a => a.Policy == ArgShortcutPolicy.NoShortcut).Count() != 0;
            var shortcutsOnly = attrs.Where(a => a.Policy == ArgShortcutPolicy.ShortcutsOnly).Count() != 0;
            var actualShortcutValues = attrs.Where(a => a.Policy == ArgShortcutPolicy.Default && a.Shortcut != null).Count() != 0;

            if (noShortcutsAllowed && shortcutsOnly) throw new InvalidArgDefinitionException("You cannot specify a policy of NoShortcut and another policy of ShortcutsOnly.");
            if (noShortcutsAllowed && actualShortcutValues) throw new InvalidArgDefinitionException("You cannot specify a policy of NoShortcut and then also specify shortcut values via another attribute.");
            if (shortcutsOnly && actualShortcutValues == false) throw new InvalidArgDefinitionException("You specified a policy of ShortcutsOnly, but did not specify any shortcuts by adding another ArgShortcut attrivute.");
        }
    }
}
