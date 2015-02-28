using System;
using System.Collections.Generic;

namespace PowerArgs
{
    internal class ConsoleWiper
    {
        public int Top { get; set; }
        public int Left { get; set; }
        public int Bottom { get; set; }

        private ConsoleString clearedLine;

        private IConsoleProvider _console;
        public IConsoleProvider Console
        {
            get { return _console; }
            set
            {
                this._console = value;
                SetTopLeftFromConsole();
                InitializeClearedLine();
            }
        }

        public ConsoleWiper() { }

        public void SetTopLeftFromConsole()
        {
            this.Top = Console.CursorTop;
            this.Left = Console.CursorLeft;
        }

        public void IncrementBottom(int amount = 1)
        {
            Bottom += amount;
        }

        public void SetBottomToTop()
        {
            Bottom = Top;
        }

        public void Wipe()
        {
            var leftNow = Console.CursorLeft;
            var topNow = Console.CursorTop;

            try
            {
                Console.CursorLeft = Left;
                Console.CursorTop = Top;
                int linesToClear = Bottom - Top;

                for (int i = 0; i < linesToClear; i++)
                {
                    if (i == 0 && Left > 0)
                    {
                        var partialLine = clearedLine.Substring(0, clearedLine.Length - Left);
                        Console.Write(partialLine);
                    }
                    else
                    {
                        Console.Write(clearedLine);
                    }
                }
            }
            finally
            {
                Console.CursorLeft = leftNow;
                Console.CursorTop = topNow;
            }
        }

        private void InitializeClearedLine()
        {
            var buffer = new List<ConsoleCharacter>();
            for (int i = 0; i < Console.BufferWidth; i++)
            {
                buffer.Add(new ConsoleCharacter(' '));
            }
            clearedLine = new ConsoleString(buffer);
        }
    }
}
