using PowerArgs;
using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public interface ILevelBuilderAction : IUndoRedoAction
    {
        LevelBuilder Context { get; set; }
    }

    public abstract class DropThingIntoLevelAction : ILevelBuilderAction
    {
        public LevelBuilder Context { get; set; }

        private ISerializableThing serializedThing;

        public void Do()
        {
            var bounds = Context.Cursor.Bounds.Clone();
            bounds.Pad(.1f);

            serializedThing = SerializeThing();
            serializedThing.Bounds = bounds;
            serializedThing.Rehydrate(true);
            Context.CurrentLevelDefinition.Things.Add(serializedThing);
            Context.PreviewScene.Add(serializedThing.HydratedThing);
        }

        public void Undo()
        {
            Context.CurrentLevelDefinition.Things.Remove(serializedThing);
            Context.PreviewScene.Remove(serializedThing.HydratedThing);
        }

        public void Redo()
        {
            serializedThing.Rehydrate(true);
            Context.CurrentLevelDefinition.Things.Add(serializedThing);
            Context.PreviewScene.Add(serializedThing.HydratedThing);
        }

        protected abstract ISerializableThing SerializeThing();
    }
}
