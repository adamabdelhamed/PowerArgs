using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// An enum to represent argument shortcut policies
    /// </summary>
    public enum ArgShortcutPolicy
    {
        /// <summary>
        /// No special behavior.
        /// </summary>
        Default,
        /// <summary>
        /// Pass this value to the ArgShortcut attribute's constructor to indicate that the given property
        /// does not support a shortcut.
        /// </summary>
        NoShortcut,
        /// <summary>
        /// This indicates that the .NET property named should not be used as an indicator.  Instead,
        /// only the values in the other ArgShortcut attributes should be used.
        /// </summary>
        ShortcutsOnly,
    }

    /// <summary>
    /// Use this attribute to override the shortcut that PowerArgs automatically assigns to each property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class ArgShortcut : Attribute
    {
        private static Dictionary<PropertyInfo, List<string>> KnownShortcuts = new Dictionary<PropertyInfo, List<string>>();
        private static List<Type> RegisteredTypes = new List<Type>();

        /// <summary>
        /// The shortcut for the given property
        /// </summary>
        public string Shortcut { get; private set; }

        /// <summary>
        /// Creates a new ArgShortcut attribute with a specified value.
        /// </summary>
        /// <param name="shortcut">The value of the new shortcut.</param>
        public ArgShortcut(string shortcut)
        {
            this.Shortcut = shortcut;
            this.Policy = ArgShortcutPolicy.Default;
        }

        /// <summary>
        /// Get the ShortcutPolicy for this attribute.
        /// </summary>
        public ArgShortcutPolicy Policy { get; private set; }

        /// <summary>
        /// Creates a new ArgShortcut using the given policy
        /// </summary>
        /// <param name="policy"></param>
        public ArgShortcut(ArgShortcutPolicy policy)
        {
            this.Policy = policy;
        }

        internal static List<string> GetShortcutsInternal(PropertyInfo info)
        {
            if (RegisteredTypes.Contains(info.DeclaringType) == false)
            {
                RegisterShortcuts(info.DeclaringType);
            }

            if (KnownShortcuts.ContainsKey(info)) return KnownShortcuts[info];
            else return new List<string>();
        }

        internal static void RegisterShortcuts(Type t, List<string> shortcutsSeenSoFar = null)
        {
            RegisteredTypes.Add(t);
            bool isNested = shortcutsSeenSoFar != null;

            shortcutsSeenSoFar = isNested ? shortcutsSeenSoFar : new List<string>();
            var actionProp = ArgAction.GetActionProperty(t);

            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (CommandLineAction.IsActionImplementation(prop) && actionProp != null) continue;

                var shortcutsForProperty = ArgShortcut.FindShortcutsInternal(prop, shortcutsSeenSoFar);
                if (shortcutsForProperty.Count > 0)
                {

                    shortcutsSeenSoFar.AddRange(shortcutsForProperty);
                    if (KnownShortcuts.ContainsKey(prop) == false)
                    {
                        KnownShortcuts.Add(prop, shortcutsForProperty);
                    }
                    else
                    {
                        KnownShortcuts[prop] = shortcutsForProperty;
                    }
                }
            }

            if (actionProp != null)
            {
                foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (CommandLineAction.IsActionImplementation(prop))
                    {
                        RegisterShortcuts(prop.PropertyType, shortcutsSeenSoFar);
                    }
                }
            }
        }

        private static List<string> FindShortcutsInternal(PropertyInfo info, List<string> knownShortcuts)
        {
            var actionProperty = ArgAction.GetActionProperty(info.DeclaringType);
            if (actionProperty != null && actionProperty.Name == info.Name) return new List<string>();

            var attrs = info.Attrs<ArgShortcut>();

            bool ignoreCase = true;
            if (info.DeclaringType.HasAttr<ArgIgnoreCase>() && info.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase == false) ignoreCase = false;

            if (attrs.Count == 0)
            {
                string shortcutVal = "";
                foreach (char c in info.GetArgumentName().Substring(0, info.GetArgumentName().Length - 1))
                {
                    shortcutVal += c;
                    if (knownShortcuts.Contains(shortcutVal) == false) return new List<string> { ignoreCase ? shortcutVal.ToLower() : shortcutVal };
                }
                return new List<string>();
            }
            else
            {
                List<string> ret = new List<string>();
                foreach (var attr in attrs.OrderBy(a => a.Shortcut == null ? 0 : a.Shortcut.Length))
                {
                    bool noShortcut = false;
                    if (attr.Policy == ArgShortcutPolicy.NoShortcut)
                    {
                        noShortcut = true;
                    }

                    if (noShortcut && attr.Shortcut != null)
                    {
                        throw new InvalidArgDefinitionException("You cannot specify a shortcut value and an ArgShortcutPolicy of NoShortcut");
                    }

                    if (attr.Shortcut != null)
                    {
                        if (attr.Shortcut.StartsWith("-")) attr.Shortcut = attr.Shortcut.Substring(1);
                        else if (attr.Shortcut.StartsWith("/")) attr.Shortcut = attr.Shortcut.Substring(1);
                    }

                    if (attr.Shortcut != null)
                    {
                        ret.Add(ignoreCase ? attr.Shortcut.ToLower() : attr.Shortcut);
                    }
                }

                return ret;
            }
        }
    }

    /// <summary>
    /// An attribute used to define long form aliases for argument
    /// names.  For example, --log-level instead of -log.
    /// It also supports an alternate syntax for providing the values.
    /// For example: --log-level=error instead of -log error or /log:error.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    [Obsolete("ArgShortcut has been refactored to support multipe shortcuts, including those that start with --.  Use [ArgShortcut(\"--my-shortcut\")]")]
    public class ArgLongForm : ArgShortcut
    {
        /// <summary>
        /// Creates a new instance of an ArgLongForm attribute given the shortcut value.
        /// </summary>
        /// <param name="value">The shortcut value</param>
        public ArgLongForm(string value) : base(Clean(value)) { }

        private static string Clean(string value)
        {
            if (value == null) return null;
            else if (value.StartsWith("--")) return value;
            else if (value.StartsWith("-")) throw new InvalidArgDefinitionException("Long form shortcuts cannot start with a single dash");
            else return "--" + value;
        }
    }
}
