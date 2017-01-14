using PowerArgs;
using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class SerializedWall : ISerializableThing
    {
        public int RehydrateOrderHint { get; set; }
        public Rectangle Bounds { get; set; }
        public ConsoleCharacter Texture { get; set; }

        public float HealthPoints { get; set; } = new Wall().HealthPoints;

        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool IsInLevelBuilderMode)
        {
            HydratedThing = new Wall() { Bounds = Bounds, Texture = Texture, HealthPoints = HealthPoints };
        }
    }

    public class DropWallAction : DropThingIntoLevelAction
    {
        protected override ISerializableThing SerializeThing()
        {
            return new SerializedWall() { Texture = Context.WallPen, HealthPoints = Context.WallPenHP };
        }
    }
}
