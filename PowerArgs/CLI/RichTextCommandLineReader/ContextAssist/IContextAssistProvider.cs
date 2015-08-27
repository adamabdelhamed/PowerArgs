using System;

namespace PowerArgs.Cli
{
    /// <summary>
    /// An interface you can implement to inject custom contextually aware command line helpers.  PowerArgs provides a few out of the box, but
    /// you are free to create your own. 
    /// </summary>
    public interface IContextAssistProvider
    {
        /// <summary>
        /// Determines if this assistant provider can assist given context about what the user has typed on the command line so far.
        /// </summary>
        /// <param name="context">context about what the user has typed on the command line so far</param>
        /// <returns>true if your provider can assist, false otherwise</returns>
        bool CanAssist(RichCommandLineContext context);

        /// <summary>
        /// Draws the provider's menu.  You can be sure that PowerArgs has called CanAssist and received a value of true before it will call this
        /// method.
        /// </summary>
        /// <param name="context">context about what the user has typed on the command line so far</param>
        /// <returns>You can choose to return a result right away if you handle keyboard input manually in your drawing function</returns>
        ContextAssistResult DrawMenu(RichCommandLineContext context);

        /// <summary>
        /// Clears the provider's menu.  This gets called if the user cancel's the assistance via the escape key or if your
        /// provider returns a terminal result when handling keyboard input.
        /// </summary>
        /// <param name="context">context about what the user has typed on the command line so far</param>
        void ClearMenu(RichCommandLineContext context);

        /// <summary>
        /// Called when your provider is visible and the user provides keyboard input.  All keys will be forwarded to your provider.  Unless you
        /// have a really good reason you should return a cancel result when you encounter the escape key and you should use the enter key as your
        /// selection mechanism.  If you returned a terminating result in your DrawMenu() function then this will never get called.
        /// </summary>
        /// <param name="context">context about what the user has typed on the command line so far</param>
        /// <param name="keyPress">Information about the key that was pressed</param>
        /// <returns>You should return the appropriate result based on how you've decided to handle the keypress.</returns>
        ContextAssistResult OnKeyboardInput(RichCommandLineContext context, ConsoleKeyInfo keyPress);
    }
}
