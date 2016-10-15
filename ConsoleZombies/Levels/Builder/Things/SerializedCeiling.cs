using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class SerializedCeiling : ISerializableThing
    {
        public int RehydrateOrderHint { get; set; }
        public Rectangle Bounds { get; set; }

        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool IsInLevelBuilderMode)
        {
            HydratedThing = new Ceiling() { Bounds = Bounds };
            if(IsInLevelBuilderMode)
            {
                (HydratedThing as Ceiling).IsVisible = true;
                HydratedThing.LifetimeManager.Manage(Scene.Current.SetTimeout(() =>
                {
                    (HydratedThing as Ceiling).IsVisible = false;
                }, TimeSpan.FromSeconds(1)));
            }
        }
    }
}
