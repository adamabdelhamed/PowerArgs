using System;

namespace PowerArgs.Cli
{
    public static class DefaultColors 
    {
        public static ConsoleColor BackgroundColor { get; set; }
        public static ConsoleColor ForegroundColor { get; set; }
        public static ConsoleColor FocusColor { get; set; }
        public static ConsoleColor FocusContrastColor { get; set; }
        public static ConsoleColor SelectedUnfocusedColor { get; set; }
        public static ConsoleColor H1Color { get; set; }
        public static ConsoleColor ButtonColor { get; set; }
        public static ConsoleColor HighlightColor { get; set; }
        public static ConsoleColor HighlightContrastColor { get; set; }
        public static ConsoleColor DisabledColor { get; set; }

        static DefaultColors()
        {
            BackgroundColor = ConsoleString.DefaultBackgroundColor;
            ForegroundColor = ConsoleString.DefaultForegroundColor;
            FocusColor = ConsoleColor.Cyan;
            FocusContrastColor = ConsoleColor.Black;
            SelectedUnfocusedColor = ConsoleColor.DarkGray;
            H1Color = ConsoleColor.Yellow;
            ButtonColor = ConsoleColor.White;
            HighlightColor = ConsoleColor.Yellow;
            HighlightContrastColor = ConsoleColor.Black;
            DisabledColor = ConsoleColor.DarkGray;
        }
    }
}
