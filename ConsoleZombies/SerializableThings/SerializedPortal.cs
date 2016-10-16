using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class SerializedPortal: ISerializableThing
    {
        public int RehydrateOrderHint { get; set; }
        public PowerArgs.Cli.Physics.Rectangle Bounds { get; set; }   
        public string DestinationId { get; set; } 

        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool isInLevelBuilder)
        {
            HydratedThing = new Portal() { Bounds = Bounds, DestinationId = DestinationId };
        }
    }


    public class DropPortalAction : ILevelBuilderAction
    {
        PowerArgs.Cli.Physics.Rectangle bounds;
        public LevelBuilder Context { get; set; }
        SerializedPortal portal;

        public void Do()
        {
            bounds = Context.Cursor.Bounds.Clone();
            Dialog.ShowTextInput("Provide portal Id".ToYellow(), (id) =>
            {
                Context.PreviewScene.QueueAction(() =>
                {
                    portal = new SerializedPortal() { Bounds = Context.Cursor.Bounds.Clone(), DestinationId = id.ToString() };
                    Context.CurrentLevelDefinition.Things.Add(portal);
                    portal.Rehydrate(true);
                    Context.PreviewScene.Add(portal.HydratedThing);
                });
            });
        }

        public void Undo()
        {
            Context.CurrentLevelDefinition.Things.Remove(portal);
            Context.PreviewScene.Remove(portal.HydratedThing);
        }

        public void Redo()
        {
            portal.Rehydrate(true);
            Context.CurrentLevelDefinition.Things.Add(portal);
            Context.PreviewScene.Add(portal.HydratedThing);
        }
    }
}
