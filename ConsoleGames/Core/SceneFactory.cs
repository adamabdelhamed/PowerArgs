using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace ConsoleGames
{
    public interface ItemReviver
    {
        bool TryRevive(LevelItem item, out SpacialElement hydratedElement);
    }

    public static class ItemReviverEx
    {
        public static string ParseTagValue(this ItemReviver reviver, string tag)
        {
            var splitIndex = tag.IndexOf(':');
            if (splitIndex <= 0) throw new ArgumentException("No tag value present for tag: "+tag);

            var val = tag.Substring(splitIndex + 1, tag.Length - (splitIndex + 1));
            return val;
        }
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
                bool hydrated = false;
                foreach(var reviver in revivers)
                {
                    if(reviver.TryRevive(item, out SpacialElement hydratedElement))
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
