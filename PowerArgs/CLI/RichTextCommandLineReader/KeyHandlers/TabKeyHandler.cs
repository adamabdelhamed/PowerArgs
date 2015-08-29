using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    /// <summary>
    /// An interface that defines the contract for tab completion in the context of a RichTextCommandLineReader.
    /// </summary>
    public interface ITabCompletionHandler
    {
        /// <summary>
        /// When the user presses the tab key in the context of a RichTextCommandLineReader's read operation, registered
        /// tab completion handlers will have this method invoked, in order.  Handlers should return true if they've updated the
        /// context by performing a successful tab completion.  They should return false if they have not.  Processing stops as soon
        /// as any handler returns true.
        /// </summary>
        /// <param name="context">Context you can use to inspect the current command line to perform tab completion</param>
        /// <returns>true if you've updated the context and performed a successful tab completion, false otherwise</returns>
        bool TryTabComplete(RichCommandLineContext context);
    }

    /// <summary>
    /// The built in tab key handler
    /// </summary>
    public class TabKeyHandler : IKeyHandler
    {
        /// <summary>
        /// gets a collection that only contains ConsoleKey.Tab
        /// </summary>
        public IEnumerable<ConsoleKey> KeysHandled
        {
            get
            {
                return new ConsoleKey[] 
                { 
                    ConsoleKey.Tab 
                };
            }
        }

        /// <summary>
        /// Gets or sets whether or not to propagate exceptions thrown by tab completion highlighters.  The default is false.
        /// </summary>
        public bool ThrowOnTabCompletionHandlerException { get; set; }

        /// <summary>
        /// Gets the list of registered tab completion handlers.
        /// </summary>
        public List<ITabCompletionHandler> TabCompletionHandlers { get; private set; }

        /// <summary>
        /// Creates a new tab key handler.
        /// </summary>
        public TabKeyHandler()
        {
            TabCompletionHandlers = new List<ITabCompletionHandler>();
        }

        /// <summary>
        /// Handles the tab key by calling all registered tab completion handlers.
        /// </summary>
        /// <param name="context">Context that can be used to inspect the current command line to perform tab completion</param>
        public void Handle(RichCommandLineContext context)
        {
            context.Intercept = true;
            context.RefreshTokenInfo();
            try
            {
                foreach (var handler in TabCompletionHandlers)
                {
                    if (handler.TryTabComplete(context))
                    {
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                if (ThrowOnTabCompletionHandlerException)
                {
                    throw;
                }
                else
                {
                    PowerLogger.LogLine("Tab completion handler threw exception: " + ex.ToString());
                }
            }
        }
    }
}
