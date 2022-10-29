using System;

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
        public static ConsoleString ToConsoleString(this string s, RGB? fg = null, RGB? bg = null, bool underlined = false)
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
        public static ConsoleString ToBlack(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.Black, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark blue, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkBlue(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.DarkBlue, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark green, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkGreen(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.DarkGreen, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark cyan, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkCyan(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.DarkCyan, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark red, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkRed(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.DarkRed, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark magenta, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkMagenta(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.DarkMagenta, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark yellow, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkYellow(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.DarkYellow, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to gray, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToGray(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.Gray, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to dark gray, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToDarkGray(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.DarkGray, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to blue, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToBlue(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.Blue, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to green, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToGreen(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.Green, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to cyan, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToCyan(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.Cyan, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to red, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToRed(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.Red, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to magenta, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToMagenta(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.Magenta, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to yellow, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToYellow(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.Yellow, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to orange, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToOrange(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.Orange, bg, underlined);
        }

        /// <summary>
        /// Changes the foreground of this string to white, and optionally forces the background of all characters to the given color.
        /// </summary>
        /// <param name="s">the string to use to create the result</param>
        /// <param name="bg">The new background color for all characters or null to use the console's default</param>
        /// <returns>a new console string with the desired color attributes</returns>
        public static ConsoleString ToWhite(this string s, RGB? bg = null, bool underlined = false)
        {
            return new ConsoleString(s, RGB.White, bg, underlined);
        }

        /// <summary>
        /// Converts any object to a console string
        /// </summary>
        /// <param name="anyObject">any object</param>
        /// <returns>a console string</returns>
        public static ConsoleString ToConsoleString(this object anyObject)
        {
            return anyObject is ICanBeAConsoleString ? (anyObject as ICanBeAConsoleString).ToConsoleString() : (anyObject + "").ToConsoleString();
        }
    }
}
