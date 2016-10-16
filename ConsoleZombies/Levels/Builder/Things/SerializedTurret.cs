using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class SerializedTurret : ISerializableThing
    {
        public int RehydrateOrderHint { get; set; }
        public Rectangle Bounds { get; set; }
        public int AmmoAmmount { get; set; } = 40;
        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool IsInLevelBuilderMode)
        {
            HydratedThing = new Turret() { AmmoAmount = AmmoAmmount, Bounds = Bounds };
        }
    }

    public class DropTurretAction : DropThingIntoLevelAction
    {
        protected override ISerializableThing SerializeThing()
        {
            return new SerializedTurret();
        }
    }
}
