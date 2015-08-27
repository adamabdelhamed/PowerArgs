using System;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that represents a search result that can be selected by the ContextAssistSearch context assist provider.
    /// </summary>
    public class ContextAssistSearchResult
    {
        /// <summary>
        /// Gets the text to display in the results view.  This text is also used when this result is inserted into the parent reader
        /// </summary>
        public string DisplayText 
        {
            get
            {
                return RichDisplayText != null ? RichDisplayText.ToString() : null;
            }
        }

        /// <summary>
        /// Gets the text to display in the results view.  This text is also used when this result is inserted into the parent reader
        /// </summary>
        public ConsoleString RichDisplayText { get; private set; }

        /// <summary>
        /// Gets a plain old .NET object that represents the underlying result value.  In many cases this will be the same as the display text.
        /// </summary>
        public object ResultValue { get; private set; }


        private ContextAssistSearchResult(object value, ConsoleString displayText)
        {
            this.ResultValue = value;
            this.RichDisplayText = displayText;
        }

        /// <summary>
        /// Creates a string result where the display text and value are the same object.
        /// </summary>
        /// <param name="stringValue">the result string</param>
        /// <returns>a search result</returns>
        public static ContextAssistSearchResult FromString(string stringValue)
        {
            return new ContextAssistSearchResult(stringValue, new ConsoleString(stringValue));
        }

        /// <summary>
        /// Creates a string result where the display text and value are the same object.
        /// </summary>
        /// <param name="stringValue">the result string</param>
        /// <returns>a search result</returns>
        public static ContextAssistSearchResult FromConsoleString(ConsoleString stringValue)
        {
            return new ContextAssistSearchResult(stringValue, stringValue);
        }

        /// <summary>
        /// Creates a search result from an object, given an optional display value.
        /// </summary>
        /// <param name="value">The object to use as a result.</param>
        /// <param name="displayText">The display text for the result.  If null, the value's ToString() method will be called</param>
        /// <returns>a search result</returns>
        public static ContextAssistSearchResult FromObject(object value, string displayText = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "value cannot be null");
            }

            return new ContextAssistSearchResult(value, new ConsoleString(displayText ?? value.ToString()));
        }

        /// <summary>
        /// Creates a search result from an object, given an optional display value.
        /// </summary>
        /// <param name="value">The object to use as a result.</param>
        /// <param name="displayText">The display text for the result.  If null, the value's ToString() method will be called</param>
        /// <returns>a search result</returns>
        public static ContextAssistSearchResult FromObject(object value, ConsoleString displayText)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "value cannot be null");
            }

            return new ContextAssistSearchResult(value, displayText ?? new ConsoleString(value.ToString()));
        }
    }
}
