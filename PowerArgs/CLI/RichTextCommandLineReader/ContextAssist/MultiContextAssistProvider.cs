using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A context assist provider that wraps a collection of inner providers.  
    /// </summary>
    public class MultiContextAssistProvider : IContextAssistProvider
    {
        /// <summary>
        /// The inner providers that will be cycled through whenever assistance is requested
        /// </summary>
        public List<IContextAssistProvider> Providers { get; private set; }

        /// <summary>
        /// Gets the current provider
        /// </summary>
        public IContextAssistProvider CurrentProvider { get; protected set; }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public MultiContextAssistProvider()
        {
            Providers = new List<IContextAssistProvider>();
        }

        /// <summary>
        /// Draws the current provider's menu
        /// </summary>
        /// <param name="context">passed to the current provider</param>
        /// <returns>the inner provider's result</returns>
        public virtual ContextAssistResult DrawMenu(RichCommandLineContext context)
        {
            return CurrentProvider.DrawMenu(context);
        }

        /// <summary>
        /// Clears the current provider's menu
        /// </summary>
        /// <param name="context">passed to the current provider</param>
        public virtual void ClearMenu(RichCommandLineContext context)
        {
            CurrentProvider.ClearMenu(context);
        }

        /// <summary>
        /// Passes the keyboard input to the current provider
        /// </summary>
        /// <param name="context">passed to the current provider</param>
        /// <param name="keyPress">passed to the current provider</param>
        /// <returns>the current provider's result</returns>
        public virtual ContextAssistResult OnKeyboardInput(RichCommandLineContext context, ConsoleKeyInfo keyPress)
        {
            return CurrentProvider.OnKeyboardInput(context, keyPress);
        }

        /// <summary>
        /// Cycles through the inner providers and calls their CanAssist method until one of them returns true.
        /// If that happens, the first to return true is promoted to be the current provider.
        /// </summary>
        /// <param name="context">passed to inner providers to see if they can assist</param>
        /// <returns>true if one of the inner providers can assist, false otherwise</returns>
        public virtual bool CanAssist(RichCommandLineContext context)
        {
            CurrentProvider = null;
            foreach (var provider in Providers)
            {
                if (provider.CanAssist(context))
                {
                    CurrentProvider = provider;
                    return true;
                }
            }

            return false;
        }
    }
}
