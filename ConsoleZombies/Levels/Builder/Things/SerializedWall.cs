using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class SerializedWall : ISerializableThing
    {
        public int RehydrateOrderHint { get; set; }
        public Rectangle Bounds { get; set; }

        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool IsInLevelBuilderMode)
        {
            HydratedThing = new Wall() { Bounds = Bounds };
        }
    }

    public class DropWallAction : DropThingIntoLevelAction
    {
        protected override ISerializableThing SerializeThing()
        {
            return new SerializedWall();
        }
    }
}
