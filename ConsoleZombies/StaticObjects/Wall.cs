using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;

namespace ConsoleZombies
{
    public class Wall : Thing, IDestructible
    {
        public float HealthPoints { get; set; }

        public ConsoleCharacter Texture { get; set; } = new ConsoleCharacter(' ', null, ConsoleColor.DarkGray);

        public Wall()
        {
            HealthPoints = 20;
        }
    }

    [ThingBinding(typeof(Wall))]
    public class WallRenderer : ThingRenderer
    {
        public WallRenderer()
        {
            this.Background = ConsoleColor.DarkGray;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = (Thing as Wall).Texture;
            context.FillRect(0, 0, Width, Height);
        }
    }
}
