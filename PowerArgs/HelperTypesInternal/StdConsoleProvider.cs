using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// Used for internal implementation, but marked public for testing, please do not use.  This is basically a wrapper around the system console.
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
            set { Console.BufferWidth = value; }
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
            if (ConsoleInDriver.Instance.IsAttached)
            {
                var c = (char)ConsoleInDriver.Instance.Read();
                ConsoleKeyInfo key;
                if(KeyMap.TryGetValue(c, out key) == false)
                {
                    key = new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false);
                }
                return key;
            }
            else
            {
                return Console.ReadKey(intercept);
            }
        }


        /// <summary>
        /// Reads a line of text from the console
        /// </summary>
        /// <returns>a line of text that was read from the console</returns>
        public string ReadLine()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Writes the given string to the console
        /// </summary>
        /// <param name="consoleString">the string to write</param>
        public void Write(ConsoleString consoleString)
        {
            var existing = ConsoleString.ConsoleProvider;
            try
            {
                ConsoleString.ConsoleProvider = this;
                consoleString.Write();
            }
            finally
            {
                ConsoleString.ConsoleProvider = existing;
            }
        }

        /// <summary>
        /// Writes the given string to the console, followed by a newline
        /// </summary>
        /// <param name="consoleString">the string to write</param>
        public void WriteLine(ConsoleString consoleString)
        {
            var existing = ConsoleString.ConsoleProvider;
            try
            {
                ConsoleString.ConsoleProvider = this;
                consoleString.WriteLine();
            }
            finally
            {
                ConsoleString.ConsoleProvider = existing;
            }
        }

        /// <summary>
        /// Writes the given character to the console
        /// </summary>
        /// <param name="consoleCharacter">the character to write</param>
        public void Write(ConsoleCharacter consoleCharacter)
        {
            var existing = ConsoleString.ConsoleProvider;
            try
            {
                ConsoleString.ConsoleProvider = this;
                consoleCharacter.Write();
            }
            finally
            {
                ConsoleString.ConsoleProvider = existing;
            }
        }

        private static Dictionary<char, ConsoleKeyInfo> KeyMap = CreateKeyMap();

        private static Dictionary<char, ConsoleKeyInfo> CreateKeyMap()
        {
            Dictionary<char, ConsoleKeyInfo> ret = new Dictionary<char, ConsoleKeyInfo>();

            for(int i = (int)'a'; i <= 'z'; i++)
            {
                var c = (char)i;
                var enumKey = Char.ToUpperInvariant(c)+"";
                var enumValue = (ConsoleKey)Enum.Parse(typeof(ConsoleKey), enumKey);
                ret.Add(c, new ConsoleKeyInfo(c, enumValue, false, false, false));
            }

            for (int i = (int)'A'; i <= 'Z'; i++)
            {
                var c = (char)i;
                var enumKey = c + "";
                var enumValue = (ConsoleKey)Enum.Parse(typeof(ConsoleKey), enumKey);
                ret.Add(c, new ConsoleKeyInfo(c, enumValue, true, false, false));
            }

            for (int i = (int)'0'; i <= '9'; i++)
            {
                var c = (char)i;
                var enumKey = "D"+c;
                var enumValue = (ConsoleKey)Enum.Parse(typeof(ConsoleKey), enumKey);
                ret.Add(c, new ConsoleKeyInfo(c, enumValue, false, false, false));
            }

            ret.Add('!', new ConsoleKeyInfo('!', ConsoleKey.D1, true, false, false));
            ret.Add('@', new ConsoleKeyInfo('@', ConsoleKey.D2, true, false, false));
            ret.Add('#', new ConsoleKeyInfo('#', ConsoleKey.D3, true, false, false));
            ret.Add('$', new ConsoleKeyInfo('$', ConsoleKey.D4, true, false, false));
            ret.Add('%', new ConsoleKeyInfo('%', ConsoleKey.D5, true, false, false));
            ret.Add('^', new ConsoleKeyInfo('^', ConsoleKey.D6, true, false, false));
            ret.Add('&', new ConsoleKeyInfo('&', ConsoleKey.D7, true, false, false));
            ret.Add('*', new ConsoleKeyInfo('*', ConsoleKey.D8, true, false, false));
            ret.Add('(', new ConsoleKeyInfo('(', ConsoleKey.D9, true, false, false));
            ret.Add(')', new ConsoleKeyInfo(')', ConsoleKey.D0, true, false, false));

            ret.Add(' ', new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));
            ret.Add('-', new ConsoleKeyInfo('-', ConsoleKey.OemMinus, false, false, false));
            ret.Add('_', new ConsoleKeyInfo('_', ConsoleKey.OemMinus, true, false, false));
            ret.Add('=', new ConsoleKeyInfo('=', ConsoleKey.OemPlus, false, false, false));
            ret.Add('+', new ConsoleKeyInfo('+', ConsoleKey.OemPlus, true, false, false));

            ret.Add('.', new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false));
            ret.Add('>', new ConsoleKeyInfo('>', ConsoleKey.OemPeriod, true, false, false));

            ret.Add(',', new ConsoleKeyInfo(',', ConsoleKey.OemComma, false, false, false));
            ret.Add('<', new ConsoleKeyInfo('<', ConsoleKey.OemComma, true, false, false));

            ret.Add('\r', new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
            ret.Add('\n', new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));
            
            return ret;
        }
    }
}
