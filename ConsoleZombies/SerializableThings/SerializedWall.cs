using PowerArgs;
using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class SerializedWall : ISerializableThing
    {
        public int RehydrateOrderHint { get; set; }
        public Rectangle Bounds { get; set; }
        public ConsoleCharacter Texture { get; set; }

        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool IsInLevelBuilderMode)
        {
            HydratedThing = new Wall() { Bounds = Bounds, Texture = Texture };
        }
    }

    public class DropWallAction : DropThingIntoLevelAction
    {
        protected override ISerializableThing SerializeThing()
        {
            return new SerializedWall() { Texture = Context.WallPen };
        }
    }
}
