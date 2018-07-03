using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;

namespace ConsoleGames
{
    public class Cursor : SpacialElement
    {
        public Cursor()
        {
            this.ResizeTo(1, 1);
        }
    }

    [SpacialElementBinding(typeof(Cursor))]
    public class CursorRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter('X', ConsoleColor.DarkCyan, ConsoleColor.Cyan);
    }
}
