using System;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// string extension methods
    /// </summary>
    public static class StringEx
    {
        /// <summary>
        /// Converts a string to a ConsoleString
        /// </summary>
        /// <param name="s">the string to convert</param>
        /// <param name="fg">the foreground color to apply to the result</param>
        /// <param name="bg">the background color to apply to the result</param>
        /// <returns>a console string</returns>
        public static ConsoleString ToConsoleString(this string s, ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            if (s == null)
            {
                return null;
            }
            else
            {
                return new ConsoleString(s, fg, bg);
            }
        }
    }
}
