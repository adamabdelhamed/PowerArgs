using PowerArgs.Cli.Physics;
using System;

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
    public class CursorRenderer : SpacialElementRenderer
    {
        public CursorRenderer()
        {
            Background = ConsoleColor.Cyan;
            ZIndex = int.MaxValue;
        }
    }
}
