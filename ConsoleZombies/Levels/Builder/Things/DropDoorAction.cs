using PowerArgs.Cli.Physics;
using System.Linq;

namespace ConsoleZombies
{
    public class SerializedDoor : ISerializableThing
    {
        public int RehydrateOrderHint { get; set; } = 100;
        public Rectangle Bounds { get; set; }
        public Rectangle AlternateBounds { get; set; }
        public bool IsOpen { get; set; }

        public Thing HydratedThing { get; private set; }

        public void Rehydrate(bool isInLevelBuilder)
        {
            var door = new Door(AlternateBounds, Bounds.Location);
            HydratedThing = door;
        }
    }

    class PositionDoorAction : ILevelBuilderAction
    {
        public bool IsOpen { get; private set; }
        public Rectangle DoorDropRectangle { get; private set; }
        public LevelBuilder Context { get; set; }
        public PositionDoorAction(bool isOpen)
        {
            this.IsOpen = isOpen;
        }

        public static bool IsReadyForDrop(LevelBuilder context)
        {
            PositionDoorAction readyPosition = null;
            foreach(var element in context.UndoStack.UndoElements.Reverse())
            {
                if(element is PositionDoorAction)
                {
                    readyPosition = element as PositionDoorAction;
                }
                else if(element is DropDoorAction)
                {
                    readyPosition = null;
                }
            }
            return readyPosition != null;
        }
      
        public void Do()
        {
            DoorDropRectangle = Context.Cursor.Bounds.Clone();
            DoorDropRectangle.Pad(.1f);
        }

        public void Undo()
        {
 
        }

        public void Redo()
        {

        }
    }

    public class DropDoorAction : DropThingIntoLevelAction
    {
        protected override ISerializableThing SerializeThing()
        {
            var placeDoorAction = (PositionDoorAction)Context.UndoStack.UndoElements.Reverse().Where(u => u is PositionDoorAction).Last();
            return new SerializedDoor()
            {
                AlternateBounds = placeDoorAction.DoorDropRectangle,
                IsOpen = placeDoorAction.IsOpen
            };
        }
    }
}
