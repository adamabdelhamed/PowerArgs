using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleGames
{
    public interface ItemReviver
    {
        bool TryRevive(LevelItem item, List<LevelItem> allItems, out SpacialElement hydratedElement);
    }

    
    public class SceneFactory
    {
        private List<ItemReviver> revivers;
        public SceneFactory(List<ItemReviver> revivers)
        {
            this.revivers = revivers;
        }

        public IEnumerable<SpacialElement> InitializeScene(Level level)
        {
            foreach(var item in level.Items)
            {
                if(item.Ignore)
                {
                    continue;
                }

                bool hydrated = false;
                foreach(var reviver in revivers)
                {
                    if(reviver.TryRevive(item, level.Items, out SpacialElement hydratedElement))
                    {
                        hydratedElement.MoveTo(item.X, item.Y);
                        hydratedElement.ResizeTo(item.Width, item.Height);
                        yield return hydratedElement;
                        hydrated = true;
                        break;
                    }
                }

                if(!hydrated)
                {
                    throw new InvalidOperationException("There was no reviver for the given item");
                }
            }
        }
    }
}
