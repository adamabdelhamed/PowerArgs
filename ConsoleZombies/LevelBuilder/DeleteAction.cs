using PowerArgs.Cli.Physics;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleZombies
{
    public class DeleteAction : ILevelBuilderAction
    {
        private Rectangle bounds;
        private List<ISerializableThing> deleted;

        public LevelBuilder Context { get; set; }


        public void Do()
        {
            bounds = Context.Cursor.Bounds.Clone();
            deleted = Context.CurrentLevelDefinition.Things.Where(t => t.Bounds.Hits(bounds)).ToList();
            Redo();
        }

        public void Undo()
        {
            foreach (var item in deleted)
            {
                item.Rehydrate(true);
                Context.CurrentLevelDefinition.Things.Add(item);
                Context.PreviewScene.Add(item.HydratedThing);
            }
        }

        public void Redo()
        {
            foreach (var element in deleted)
            {
                Context.CurrentLevelDefinition.Things.Remove(element);
            }
            
            foreach (var element in Context.PreviewScene.Things
                .Where(t => t is Cursor == false && t.Bounds.Hits(bounds)).ToList())
            {
                Context.PreviewScene.Remove(element);
            }
        }
    }
}
