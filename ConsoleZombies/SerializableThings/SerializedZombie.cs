using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class SerializedZombie : ISerializableThing
    {
        public int RehydrateOrderHint { get; set; } 
        public Rectangle Bounds { get; set; }

        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool IsInLevelBuilderMode)
        {
            HydratedThing = new Zombie() { Bounds = Bounds };
            if(IsInLevelBuilderMode == false)
            {
                (HydratedThing as Zombie).IsActive = true;
            }
        }
    }

    public class DropZombieAction : DropThingIntoLevelAction
    {
        protected override ISerializableThing SerializeThing()
        {
            return new SerializedZombie();
        }
    }
}
