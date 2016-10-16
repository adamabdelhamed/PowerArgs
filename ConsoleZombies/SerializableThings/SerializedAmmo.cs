using System;
using PowerArgs.Cli.Physics;
using System.Linq;
using System.Collections.Generic;
using PowerArgs.Cli;
using PowerArgs;
using System.Reflection;

namespace ConsoleZombies
{
    public class SerializedAmmo : ISerializableThing
    {
        public int RehydrateOrderHint { get; set; }
        public PowerArgs.Cli.Physics.Rectangle Bounds { get; set; }
   
        public string AmmoType { get; set; }
        public int Amount { get; set; }   

        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool isInLevelBuilder)
        {
            var ammo = (Ammo) Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType(AmmoType));
            ammo.Amount = Amount;
            ammo.Bounds = Bounds;
            HydratedThing = ammo;
        }
    }


    public class DropAmmoAction : ILevelBuilderAction
    {
        private PowerArgs.Cli.Physics.Rectangle bounds;
        private SerializedAmmo ammo;

        public LevelBuilder Context { get; set; }


        public void Do()
        {
            bounds = Context.Cursor.Bounds.Clone();

            Dialog.Pick("Choose Ammo Type".ToConsoleString(),
                Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.HasAttr<AmmoInfo>())
                .Select(t => new DialogOption() { DisplayText = t.Attr<AmmoInfo>().DisplayName.ToConsoleString(), Id = t.FullName }))
                .Then((choice) =>
                {
                    Context.PreviewScene.QueueAction(() =>
                    {
                        int amount = 10;
                        bounds = Context.Cursor.Bounds.Clone();
                        bounds.Pad(.1f);
                        this.ammo = new SerializedAmmo() { AmmoType = choice.Id, Amount = amount, Bounds = bounds };

                        Context.CurrentLevelDefinition.Things.Add(ammo);
                        ammo.Rehydrate(true);
                        Context.PreviewScene.Add(ammo.HydratedThing);
                    });
                });
        }

        public void Undo()
        {
            Context.CurrentLevelDefinition.Things.Remove(ammo);
            Context.PreviewScene.Remove(ammo.HydratedThing);
        }

        public void Redo()
        {
            ammo.Rehydrate(true);
            Context.CurrentLevelDefinition.Things.Add(ammo);
            Context.PreviewScene.Add(ammo.HydratedThing);
        }
    }
}
