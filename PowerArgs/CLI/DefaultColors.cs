using System;

namespace PowerArgs.Cli
{
    public static class DefaultColors 
    {
        public static RGB BackgroundColor { get; set; }
        public static RGB ForegroundColor { get; set; }
        public static RGB FocusColor { get; set; }
        public static RGB FocusContrastColor { get; set; }
        public static RGB SelectedUnfocusedColor { get; set; }
        public static RGB H1Color { get; set; }
        public static RGB ButtonColor { get; set; }
        public static RGB HighlightColor { get; set; }
        public static RGB HighlightContrastColor { get; set; }
        public static RGB DisabledColor { get; set; }

        static DefaultColors()
        {
            BackgroundColor = ConsoleString.DefaultBackgroundColor;
            ForegroundColor = ConsoleString.DefaultForegroundColor;
            FocusColor = RGB.Cyan;
            FocusContrastColor = RGB.Black;
            SelectedUnfocusedColor = RGB.DarkGray;
            H1Color = RGB.Yellow;
            ButtonColor = RGB.White;
            HighlightColor = RGB.Yellow;
            HighlightContrastColor = RGB.Black;
            DisabledColor = RGB.DarkGray;
        }
    }
}
