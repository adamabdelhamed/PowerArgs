
using System;
namespace PowerArgs
{
    /// <summary>
    /// An interface used to implement custom tab completion logic.
    /// </summary>
    [Obsolete("Use ISmartTabCompletionSource.  It gives you much better context you can use for tab completion.")]
    public interface ITabCompletionSource
    {
        /// <summary>
        /// PowerArgs will call this method if it has enhanced the command prompt and the user presses tab.  You should use the
        /// text the user has types so far to determine if there is a completion you'd like to make.  If you find a completion
        /// then you should assign it to the completion variable and return true.
        /// </summary>
        /// <param name="shift">Indicates if shift was being pressed</param>
        /// <param name="soFar">The text token that the user has typed before pressing tab.</param>
        /// <param name="completion">The variable that you should assign the completed string to if you find a match.</param>
        /// <returns>True if you completed the string, false otherwise.</returns>
        bool TryComplete(bool shift, string soFar, out string completion);
    }

    /// <summary>
    /// A replacement for ITabCompletionSource that makes it easier to implement custom tab completion logic
    /// </summary>
    public interface ISmartTabCompletionSource
    {
        /// <summary>
        /// PowerArgs will call this method if it has enhanced the command prompt and the user presses tab.  The 
        /// context object passed into the function will contain useful information about what the user has typed on the command line.
        /// If you find a completion then you should assign it to the completion variable and return true.
        /// </summary>
        /// <param name="context">An object containing useful information about what the user has typed on the command line</param>
        /// <param name="completion">The variable that you should assign the completed string to if you find a match.</param>
        /// <returns>True if you completed the string, false otherwise.</returns>
        bool TryComplete(TabCompletionContext context, out string completion);
    }

    public class TabCompletionContext
    {
        public bool Shift { get; set; }
        public string PreviousToken { get; set; }
        public string CommandLineText { get; set; }
        public int Position { get; set; }
        public string CompletionCandidate { get; set; }
        public CommandLineArgument TargetArgument { get; set; }
        public CommandLineAction TargetAction { get; set; }

        public CommandLineArgumentsDefinition Definition { get; set; }
    }
}
