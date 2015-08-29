using System.Collections.Generic;

namespace PowerArgs.Cli
{
    /// <summary>
    /// Status codes that content assist providers can return
    /// </summary>
    public enum ContextAssistResultStatusCode
    {
        /// <summary>
        /// Indicates that no progress was made and that the assist provider should continue to accept keyboard input.
        /// </summary>
        NoOp,
        /// <summary>
        /// Indicates that progress was made and that the value of the result object's NewBuffer property should be used to replace the parent reader's buffer.
        /// </summary>
        Success,
        /// <summary>
        /// Indicates that the user or system cancelled the assist provider and the parent buffer should not be modified. 
        /// </summary>
        Cancel,
    }

    /// <summary>
    /// A class that represents a result that an IContentAssistProvider returns to a parent reader.
    /// </summary>
    public class ContextAssistResult
    {
        /// <summary>
        /// Gets status code for this result
        /// </summary>
        public ContextAssistResultStatusCode StatusCode { get; private set; }

        /// <summary>
        /// Gets the new buffer to apply to the parent reader.  This is only applicable for a status code of Success.
        /// </summary>
        public List<ConsoleCharacter> NewBuffer { get; private set; }

        /// <summary>
        /// Gets the offset to apply to the cursor position.  This is only applicable for a status code of Success.
        /// </summary>
        public int ConsoleRefreshLeftOffset { get; private set; }

        /// <summary>
        /// Returns true if this result should stop the current assist provider, false otherwise
        /// </summary>
        public bool IsTerminal
        {
            get
            {
                return StatusCode == ContextAssistResultStatusCode.Success || StatusCode == ContextAssistResultStatusCode.Cancel;
            }
        }

        private ContextAssistResult() { }

        /// <summary>
        /// A result that indicates that no result is ready and that the assist provider should continue to accept keyboard input
        /// </summary>
        public static readonly ContextAssistResult NoOp = new ContextAssistResult() { StatusCode = ContextAssistResultStatusCode.NoOp };

        /// <summary>
        /// A result that indicates that either the user or the system wants to stop the assist provider without making any changes to the 
        /// parent reader
        /// </summary>
        public static readonly ContextAssistResult Cancel = new ContextAssistResult() { StatusCode = ContextAssistResultStatusCode.Cancel };

        /// <summary>
        /// Creates a custom result that manually replaces the entire parent reader's buffer.
        /// </summary>
        /// <param name="newBuffer">the new buffer to apply to the parent reader</param>
        /// <param name="consoleRefreshLeftOffset">The relative offset to apply to the current cursor position</param>
        /// <returns>a custom result that manually replaces the entire parent reader's buffer</returns>
        public static ContextAssistResult CreateCustomResult(List<ConsoleCharacter> newBuffer, int consoleRefreshLeftOffset)
        {
            return new ContextAssistResult() { NewBuffer = newBuffer, StatusCode = ContextAssistResultStatusCode.Success, ConsoleRefreshLeftOffset = consoleRefreshLeftOffset };
        }

        /// <summary>
        /// Creates a result that replaces the current token with the given selection.
        /// </summary>
        /// <param name="context">Context from the parent reader</param>
        /// <param name="selection">The selection string to insert</param>
        /// <returns>a result that replaces the current token with the given selection</returns>
        public static ContextAssistResult CreateInsertResult(RichCommandLineContext context, ConsoleString selection)
        {
            context.RefreshTokenInfo();
            var ret = new ContextAssistResult();

            bool hasInserted = false;
            var newBuffer = new List<ConsoleCharacter>();
            foreach (var token in context.Tokens)
            {
                if (context.IsCursorOnToken(token))
                {
                    if (string.IsNullOrWhiteSpace(token.Value))
                    {
                        newBuffer.AddRange(context.GetBufferSubstringFromToken(token));
                        ret.ConsoleRefreshLeftOffset = selection.Length;
                    }
                    else
                    {
                        var tokenOffset = context.BufferPosition - token.StartIndex;
                        ret.ConsoleRefreshLeftOffset = selection.Length - tokenOffset;
                    }

                    if (hasInserted == false)
                    {
                        hasInserted = true;
                        // cursor is on the current token
                        newBuffer.AddRange(selection);
                    }
                }
                else
                {
                    // this token not be modified
                    newBuffer.AddRange(context.GetBufferSubstringFromToken(token));
                }
            }

            if (hasInserted == false)
            {
                hasInserted = true;
                // cursor is on the current token
                newBuffer.AddRange(selection);
                ret.ConsoleRefreshLeftOffset = selection.Length;
            }

            ret.StatusCode = ContextAssistResultStatusCode.Success;
            ret.NewBuffer = newBuffer;
            return ret;
        }
    }
}
