using System;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A progress bar designed for use with the console bitmap player.  It shows the current play cursor and indicates loading progress
    /// </summary>
    internal class PlayerProgressBar : ConsoleControl
    {
        /// <summary>
        /// The current position of the loading indicator (0 to 1)
        /// </summary>
        public double LoadProgressPosition { get { return Get<double>(); } set { Set(value); } }

        /// <summary>
        /// The current position of the play cursor (0 to 1)
        /// </summary>
        public double PlayCursorPosition { get { return Get<double>(); } set { Set(value); } }

        /// <summary>
        /// The color of the portion of the bar that represents loaded content, defaults to white
        /// </summary>
        public ConsoleColor LoadingProgressColor { get { return Get<ConsoleColor>(); } set { Set(value); } }

        /// <summary>
        /// True if you want to render the play cursor, false otherwise
        /// </summary>
        public bool ShowPlayCursor { get { return Get<bool>(); } set { Set(value); } }

        /// <summary>
        /// The color of the play cursor, defaults to green
        /// </summary>
        public ConsoleColor PlayCursorColor { get { return Get<ConsoleColor>(); } set { Set(value); } }

        public PlayerProgressBar()
        {
            this.Height = 1;
            this.Background = ConsoleColor.Gray;
            this.LoadingProgressColor = ConsoleColor.White;
            this.PlayCursorColor = ConsoleColor.Green;
            this.ShowPlayCursor = true;
            this.CanFocus = false;
        }

        /// <summary>
        /// Paints the progress bar
        /// </summary>
        /// <param name="context"></param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            var loadProgressPixels = (int)(0.5 + (LoadProgressPosition * Width));
            var playCursorOffset = (int)(0.5 + (PlayCursorPosition * Width));
            if (playCursorOffset == Width) playCursorOffset--;

            // draws the loading progress
            context.FillRect(new ConsoleCharacter(' ', null, LoadingProgressColor), 0, 0, loadProgressPixels, 1);

            if (ShowPlayCursor)
            {
                // draws the play cursor
                context.DrawPoint(new ConsoleCharacter(' ', null, PlayCursorColor), playCursorOffset, 0);
            }
        }
    }
}
