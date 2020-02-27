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
        public static ConsoleString ToConsoleString(this string s, ConsoleColor? fg = null, ConsoleColor? bg = null, bool underlined = false)
        {
            if (s == null)
            {
                return null;
            }
            else
            {
                return new ConsoleString(s, fg, bg, underlined);
            }
        }

        /// <summary>
        /// Changes the foreground of this string to black, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToBlack(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.Black, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark blue, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkBlue(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.DarkBlue, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark green, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkGreen(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.DarkGreen, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark cyan, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkCyan(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.DarkCyan, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark red, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkRed(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.DarkRed, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark magenta, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkMagenta(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.DarkMagenta, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark yellow, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkYellow(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.DarkYellow, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to gray, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToGray(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.Gray, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark gray, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkGray(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.DarkGray, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to blue, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToBlue(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.Blue, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to green, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToGreen(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.Green, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to cyan, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToCyan(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.Cyan, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to red, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToRed(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.Red, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to magenta, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToMagenta(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.Magenta, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to yellow, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToYellow(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.Yellow, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to white, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToWhite(this string s, ConsoleColor? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, ConsoleColor.White, bg, underlined);
        }
    }
}
