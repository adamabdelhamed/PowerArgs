using System;

namespace PowerArgs
{
    /// <summary>
    /// An interface that serves as an abstraction layer for a console implementation.  
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
        /// Gets or sets the left position of the console cursor
        /// </summary>
        int CursorLeft { get; set; }

        /// <summary>
        /// Gets or sets the top position of the console cursor
        /// </summary>
        int CursorTop { get; set; }

        /// <summary>
        /// Gets the buffer width of the console
        /// </summary>
        int BufferWidth { get; set; }

        /// <summary>
        /// Write's the string representation of the given object to the console
        /// </summary>
        void Write(object output);

        /// <summary>
        /// Write's the string representation of the given object to the console, followed by a newline.
        /// </summary>
        void WriteLine(object output);

        /// <summary>
        /// Writes the given console string to the console, preserving formatting
        /// </summary>
        /// <param name="consoleString">The string to write</param>
        void Write(ConsoleString consoleString);

        /// <summary>
        /// Writes the given character to the console, preserving formatting
        /// </summary>
        /// <param name="consoleCharacter">The character to write</param>
        void Write(ConsoleCharacter consoleCharacter);

        /// <summary>
        /// Writes the given console string to the console, followed by a newline, preserving formatting.
        /// </summary>
        /// <param name="consoleString">The string to write</param>
        void WriteLine(ConsoleString consoleString);

        /// <summary>
        /// Writes a newline to the console
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
