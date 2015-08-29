using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that stores and manages command line history that is used by the up and down arrow keys handler to let users cycle through historical command lines.
    /// </summary>
    public class ConsoleHistoryManager
    {
        internal int Index { get; set; }

        /// <summary>
        /// Gets the list of values that can be cycled through
        /// </summary>
        public List<ConsoleString> Values { get; private set; }

        internal ConsoleHistoryManager()
        {
            Values = new List<ConsoleString>();
            Index = -1;
        }
    }
}
