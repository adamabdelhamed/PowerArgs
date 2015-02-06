using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// An interface that defines the contract for how key presses get handled within the RichTextCommandLineReader context
    /// </summary>
    public interface IKeyHandler
    {
        /// <summary>
        /// Gets the list of keys that are handled by this handler
        /// </summary>
        IEnumerable<ConsoleKey> KeysHandled { get; }

        /// <summary>
        /// This will be called when the user presses one of the keys defined by KeysHandled.
        /// </summary>
        /// <param name="context">Context that lets you inspect the current state of the command line and modify it</param>
        void Handle(RichCommandLineContext context);
    }
}
