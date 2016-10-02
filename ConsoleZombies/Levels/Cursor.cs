using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class Cursor : Thing
    {
        public Cursor()
        {
            Bounds = new Rectangle(0, 0, 1, 1);
        }
    }

    [ThingBinding(typeof(Cursor))]
    public class CursorRenderer : ThingRenderer
    {
        public CursorRenderer()
        {
            Background = ConsoleColor.Cyan;
            ZIndex = 10000;
        }
    }
}
