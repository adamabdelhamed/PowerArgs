using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;

namespace PowerArgs.Games
{
    public class Cursor : SpacialElement
    {
        public Cursor()
        {
            this.ResizeTo(1, 1);
            this.MoveTo(0, 0, int.MaxValue);
        }
    }

    [SpacialElementBinding(typeof(Cursor))]
    public class CursorRenderer : SpacialElementRenderer
    {
        private ConsoleString DefaultStyle => new ConsoleString("X", ConsoleColor.DarkCyan, ConsoleColor.Cyan);
        protected override void OnPaint(ConsoleBitmap context) => context.DrawString(DefaultStyle, 0, 0);
    }
}
