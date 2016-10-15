using PowerArgs.Cli.Physics;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleZombies
{
    class DropAutoCeilingAction : ILevelBuilderAction
    {
        public Rectangle PreviewRectangle { get; private set; }
        public LevelBuilder Context { get; set; }

        private List<SerializedCeiling> previewTiles = new List<SerializedCeiling>();

        public void Do()
        {
            PreviewRectangle = Context.Cursor.Bounds.Clone();
            PreviewRectangle.Pad(.1f);
            Redo();
        }

        public void Undo()
        {
            foreach (var tile in previewTiles)
            {
                Context.CurrentLevelDefinition.Things.Remove(tile);
                Context.PreviewScene.Remove(tile.HydratedThing);
            }
        }

        public void Redo()
        {
            Rectangle position = PreviewRectangle.Clone();
            float y = position.Y;
            float? rightEdge = null;
            while (Context.PreviewScene.Bounds.Contains(position) && IsEmptyCeilingSpace(position))
            {
                while (Context.PreviewScene.Bounds.Contains(position) && IsEmptyCeilingSpace(position) && (rightEdge.HasValue == false || (position.X <= rightEdge.Value)))
                {
                    var tile = new SerializedCeiling() { Bounds = position.Clone() };
                    previewTiles.Add(tile);
                    tile.Rehydrate(true);
                    Context.CurrentLevelDefinition.Things.Add(tile);
                    Context.PreviewScene.Add(tile.HydratedThing);
                    position.MoveBy(Context.ScenePanel.PixelSize.W, 0);
                }

                if(rightEdge.HasValue == false)
                {
                    rightEdge = position.X - Context.ScenePanel.PixelSize.W;
                }

                position = PreviewRectangle.Clone();
                position.MoveTo(new Location(position.X, ++y));
            }
        }

        private bool IsEmptyCeilingSpace(Rectangle position)
        {
            var thingsThatTouch = Context.PreviewScene.Things.Where(t => t.GetType() == typeof(Wall) && t.Bounds.Hits(position));
            return thingsThatTouch.Count() == 0;
        }
    }
}