using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// Extension methods for IConsoleProvider
    /// </summary>
    public static class IConsoleProviderEx
    {
        /// <summary>
        /// Takes a snapshot that stores the console cursor's current position.
        /// </summary>
        /// <param name="console">the console to target</param>
        /// <returns>a snapshot that stores the console cursor's current position.</returns>
        public static ConsoleSnapshot TakeSnapshot(this IConsoleProvider console)
        {
            if (console == null)
            {
                throw new ArgumentNullException("console was null");
            }

            return new ConsoleSnapshot(console);
        }
    }
}
