using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
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

    /// <summary>
    /// A class that lets you dynamically create key handlers
    /// </summary>
    public class KeyHandler : IKeyHandler
    {
        /// <summary>
        /// Gets the keys handled by this handler
        /// </summary>
        public IEnumerable<ConsoleKey> KeysHandled{get;private set;}

        /// <summary>
        /// gets the action that implements the handler functionality
        /// </summary>
        public Action<RichCommandLineContext> Handler { get; private set; }

        private KeyHandler(IEnumerable<ConsoleKey> keys, Action<RichCommandLineContext> handler) 
        {
            this.KeysHandled = keys;
            this.Handler = handler;
        }

        /// <summary>
        /// Creates a key handler from the given action
        /// </summary>
        /// <param name="handler">the handler action code</param>
        /// <param name="keysHandled">the keys that this handler handles</param>
        /// <returns>the handler</returns>
        public static IKeyHandler FromAction(Action<RichCommandLineContext> handler, params ConsoleKey[] keysHandled)
        {
            return new KeyHandler(keysHandled, handler);
        }

        /// <summary>
        /// Calls the handler action code
        /// </summary>
        /// <param name="context">context from the parent reader</param>
        public void Handle(RichCommandLineContext context)
        {
            Handler(context);
        }
    }
}
