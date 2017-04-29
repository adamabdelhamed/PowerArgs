using PowerArgs.Cli;
using System;

namespace PowerArgs.Cli
{
    public class PlayerProgressBar : ConsoleControl
    {
        public double LoadProgressPosition { get { return Get<double>(); } set { Set(value); } }
        public double PlayCursorPosition { get { return Get<double>(); } set { Set(value); } }
        public ConsoleColor LoadingProgressColor { get { return Get<ConsoleColor>(); } set { Set(value); } }
        public bool ShowPlayCursor { get { return Get<bool>(); } set { Set(value); } }
        public ConsoleColor PlayCursorColor { get { return Get<ConsoleColor>(); } set { Set(value); } }

        public PlayerProgressBar()
        {
            this.Height = 1;
            this.Background = ConsoleColor.Gray;
            this.LoadingProgressColor = ConsoleColor.White;
            this.PlayCursorColor = ConsoleColor.Cyan;
            this.ShowPlayCursor = true;
            this.CanFocus = false;
        }

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
