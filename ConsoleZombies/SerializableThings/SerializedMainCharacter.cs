using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class SerializedMainCharacter : ISerializableThing
    {
        public int RehydrateOrderHint { get; set; } = -1;
        public Rectangle Bounds { get; set; }

        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool IsInLevelBuilderMode)
        {
            HydratedThing = new MainCharacter() { IsInLevelBuilder = IsInLevelBuilderMode, Bounds = Bounds };
        }
    }

    public class DropMainCharacterAction : DropThingIntoLevelAction
    {
        protected override ISerializableThing SerializeThing()
        {
            return new SerializedMainCharacter();
        }
    }
}
