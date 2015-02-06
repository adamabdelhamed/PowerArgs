
namespace PowerArgs
{
    /// <summary>
    /// An interface that defines the contract for how syntax highlighting is performed
    /// </summary>
    public interface ISyntaxHighlighter
    {
        /// <summary>
        /// This method will be called by the RichTextCommandLineReader after each keypress.  All highlighters will be called.
        /// </summary>
        /// <param name="context">Context that lets you inspect the current state of the command line.</param>
        /// <returns>true if you performed any highlighting modifications, false otherwise</returns>
        bool TryHighlight(RichCommandLineContext context);
    }
}
