using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// Used for internal implementation, but marked public for testing, please do not use.
    /// </summary>
    public class StdConsoleProvider : IConsoleProvider
    {
        const int STD_OUTPUT_HANDLE = -11;

        /// <summary>
        /// Gets or sets the console foreground color
        /// </summary>
        public ConsoleColor ForegroundColor
        {
            get
            {
                return Console.ForegroundColor;
            }
            set
            {
                Console.ForegroundColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the console background color
        /// </summary>
        public ConsoleColor BackgroundColor
        {
            get
            {
                return Console.BackgroundColor;
            }
            set
            {
                Console.BackgroundColor = value;
            }
        }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        public int CursorLeft
        {
            get
            {
                return Console.CursorLeft;
            }
            set
            {
                Console.CursorLeft = value;
            }
        }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        public int CursorTop
        {
            get
            {
                return Console.CursorTop;
            }
            set
            {
                Console.CursorTop = value;
            }
        }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        public int BufferWidth
        {
            get { return Console.BufferWidth; }
        }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        /// <returns>Used for internal implementation, but marked public for testing, please do not use.</returns>
        public ConsoleKeyInfo ReadKey()
        {
            return Console.ReadKey(true);
        }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        /// <param name="output">Used for internal implementation, but marked public for testing, please do not use.</param>
        public void Write(object output)
        {
            Console.Write(output);
        }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        /// <param name="output">Used for internal implementation, but marked public for testing, please do not use.</param>
        public void WriteLine(object output)
        {
            Console.WriteLine(output);
        }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        public void WriteLine()
        {
            Console.WriteLine();
        }

        [DllImport("Kernel32", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("Kernel32", SetLastError = true)]
        static extern bool ReadConsoleOutputCharacter(IntPtr hConsoleOutput,
            [Out] StringBuilder lpCharacter, uint nLength, COORD dwReadCoord,
            out uint lpNumberOfCharsRead);

        [StructLayout(LayoutKind.Sequential)]
        struct COORD
        {
            public short X;
            public short Y;
        }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        /// <param name="y">Used for internal implementation, but marked public for testing, please do not use.</param>
        /// <returns>Used for internal implementation, but marked public for testing, please do not use.</returns>
        public static string ReadALineOfConsoleOutput(int y)
        {
            if (y < 0) throw new Exception();
            IntPtr stdout = GetStdHandle(STD_OUTPUT_HANDLE);

            uint nLength = (uint)Console.WindowWidth;
            StringBuilder lpCharacter = new StringBuilder((int)nLength);

            // read from the first character of the first line (0, 0).
            COORD dwReadCoord;
            dwReadCoord.X = 0;
            dwReadCoord.Y = (short)y;

            uint lpNumberOfCharsRead = 0;

            if (!ReadConsoleOutputCharacter(stdout, lpCharacter, nLength, dwReadCoord, out lpNumberOfCharsRead))
                throw new Win32Exception();

            var str = lpCharacter.ToString();
            str = str.Substring(0, str.Length - 1).Trim();

            return str;
        }

        /// <summary>
        /// Clears the console
        /// </summary>
        public void Clear()
        {
            Console.Clear();
        }

        /// <summary>
        /// Reads the next character of input from the console
        /// </summary>
        /// <returns>the char or -1 if there is no more input</returns>
        public int Read()
        {
            return Console.Read();
        }

        /// <summary>
        /// Reads a key from the console
        /// </summary>
        /// <param name="intercept">if true, intercept the key before it is shown on the console</param>
        /// <returns>info about the key that was pressed</returns>
        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            return Console.ReadKey(intercept);
        }


        /// <summary>
        /// Reads a line of text from the console
        /// </summary>
        /// <returns>a line of text that was read from the console</returns>
        public string ReadLine()
        {
            throw new NotImplementedException();
        }
    }
}
