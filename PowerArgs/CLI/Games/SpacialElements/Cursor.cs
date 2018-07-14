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
    public class CursorRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter('X', ConsoleColor.DarkCyan, ConsoleColor.Cyan);
    }
}
