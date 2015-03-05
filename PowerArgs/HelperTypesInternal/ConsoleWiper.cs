using System;
using System.Collections.Generic;

namespace PowerArgs
{
    internal class ConsoleWiper : IDisposable
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

        public ConsoleWiper(ConsoleSnapshot snapshot)
        {
            this.Console = snapshot.Console;
            this.Top = snapshot.Top;
            this.Left = snapshot.Left;
        }

        public void MoveCursorToLineAfterBottom()
        {
            Console.CursorLeft = 0;
            Console.CursorTop = Bottom + 1;
        }

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
            using (Console.TakeSnapshot())
            {
                Console.CursorLeft = Left;
                Console.CursorTop = Top;
                int linesToClear = (Bottom - Top)+1;

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

        ~ConsoleWiper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (Console != null)
                {
                    Wipe();
                }
            }
        }
    }
}
