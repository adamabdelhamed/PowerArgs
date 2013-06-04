using System;

namespace PowerArgs
{
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
