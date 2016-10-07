using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class Wall : Thing, IDestructible
    {
        public float HealthPoints { get; set; }

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
    }
}
