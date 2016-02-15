using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class Theme
    {
        public ConsoleColor BackgroundColor { get; set; }
        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor FocusColor { get; set; }
        public ConsoleColor FocusContrastColor { get; set; }
        public ConsoleColor SelectedUnfocusedColor { get; set; }
        public ConsoleColor H1Color { get; set; }
        public ConsoleColor ButtonColor { get; set; }
        public ConsoleColor HighlightColor { get; set; }
        public ConsoleColor HighlightContrastColor { get; set; }
        public ConsoleColor DisabledColor { get; set; }
        public static readonly Theme DefaultTheme = new Theme();

        public Theme()
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
