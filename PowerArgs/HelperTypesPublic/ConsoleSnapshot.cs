using System;

namespace PowerArgs
{
    /// <summary>
    /// An object that tracks and restores the cursor on a console
    /// </summary>
    public class ConsoleSnapshot : IDisposable
    {
        /// <summary>
        /// Gets or sets the left position of the snapshot
        /// </summary>
        public int Left { get; set; }
        /// <summary>
        /// Gets or sets the top position of the snapshot
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// Gets a reference to the console this snapshot was taken from
        /// </summary>
        public IConsoleProvider Console { get; private set; }

        /// <summary>
        /// Creates a snapshot with explicit cursor position values
        /// </summary>
        /// <param name="left">the left position of the snapshot</param>
        /// <param name="top">the top position of the snapshot</param>
        /// <param name="console">the target console</param>
        public ConsoleSnapshot(int left, int top, IConsoleProvider console)
        {
            this.Left = left;
            this.Top = top;
            this.Console = console;
        }

        /// <summary>
        /// Creates a snapshot from the given console's current position
        /// </summary>
        /// <param name="console">the target console</param>
        public ConsoleSnapshot(IConsoleProvider console)
        {
            this.Left = console.CursorLeft;
            this.Top = console.CursorTop;
            this.Console = console;
        }

        /// <summary>
        /// Creates and returns a new snapshot that is offset from the current 
        /// snapshot
        /// </summary>
        /// <param name="xOffset">the delta from this snapshot's Left value</param>
        /// <param name="yOffset">the delta from this snapshot's Top value</param>
        /// <returns> a new snapshot that is offset from the current snapshot</returns>
        public ConsoleSnapshot CreateOffsetSnapshot(int xOffset, int yOffset)
        {
            var ret = new ConsoleSnapshot(Left + xOffset, Top + yOffset, this.Console);
            return ret;
        }

        /// <summary>
        /// Restores the target console to this snapshot's cursor position
        /// </summary>
        public void Restore()
        {
            this.Console.CursorLeft = Left;
            this.Console.CursorTop = Top;
        }

        /// <summary>
        /// restores the snapshot
        /// </summary>
        ~ConsoleSnapshot()
        {
            Dispose(false);
        }

        /// <summary>
        /// Restores the target console to this snapshot's cursor position
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Restores the target console to this snapshot's cursor position
        /// </summary>
        /// <param name="disposing">used for correct dispose pattern impl</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (Console != null)
                {
                    Restore();
                    Console = null;
                }
            }
        }
    }
}
