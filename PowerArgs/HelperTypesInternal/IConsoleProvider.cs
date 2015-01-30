using System;

namespace PowerArgs
{
    /// <summary>
    /// Used for internal implementation, but marked public for testing, please do not use.
    /// </summary>
    public interface IConsoleProvider
    {
        /// <summary>
        /// Gets or sets the foreground color
        /// </summary>
        ConsoleColor ForegroundColor { get; set; }
        /// <summary>
        /// Gets or sets the backgrund color
        /// </summary>
        ConsoleColor BackgroundColor { get; set; }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        int CursorLeft { get; set; }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        int CursorTop { get; set; }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        int BufferWidth { get; }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        void Write(object output);

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        void WriteLine(object output);

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        void WriteLine();

        /// <summary>
        /// Clears the console window
        /// </summary>
        void Clear();

        /// <summary>
        /// Reads the next character of input from the console
        /// </summary>
        /// <returns>the char or -1 if there is no more input</returns>
        int Read();

        /// <summary>
        /// Reads a key from the console
        /// </summary>
        /// <param name="intercept">if true, intercept the key before it is shown on the console</param>
        /// <returns>info about the key that was pressed</returns>
        ConsoleKeyInfo ReadKey(bool intercept);

        /// <summary>
        /// Reads a key from the console
        /// </summary>
        ConsoleKeyInfo ReadKey();

        /// <summary>
        /// Reads a line of text from the console
        /// </summary>
        /// <returns>a line of text that was read from the console</returns>
        string ReadLine();
    }
}
