using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs.Games
{
    public interface ItemReviver
    {
        bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement);
    }

    
    public class SceneFactory
    {
        private List<ItemReviver> revivers;
        public SceneFactory(List<ItemReviver> revivers)
        {
            this.revivers = revivers;
        }

        public static T CreateInstance<T>(string name)
        {
            var candidate = Assembly.GetExecutingAssembly().ExportedTypes.Where(t => t.Name.ToLower() == name.ToLower()).FirstOrDefault();

            if (candidate == null)
            {
                candidate = Assembly.GetEntryAssembly().ExportedTypes.Where(t => t.Name.ToLower() == name.ToLower()).FirstOrDefault();
            }

            if (candidate == null)
            {
                throw new ArgumentException("Could not resolve type: " + name);
            }

            return (T)Activator.CreateInstance(candidate);
        }

        public IEnumerable<ITimeFunction> InitializeScene(Level level)
        {
            foreach(var item in level.Items.OrderBy(i => i.HasValueTag("trigger") ? 0 : 1).ThenBy(i => i.Y).ThenBy(i => i.X))
            {
                if(item.Ignore)
                {
                    continue;
                }

                bool hydrated = false;
                foreach(var reviver in revivers)
                {
                    if(item.Ignore)
                    {
                        continue;
                    }

                    var reviveResult = reviver.TryRevive(item, level.Items, out ITimeFunction function);
                    if (reviveResult)
                    {
                        if(function is TimeFunction)
                        {
                            (function as TimeFunction).AddTags(item.Tags);
                        }

                        if (function is SpacialElement)
                        {
                            var hydratedElement = function as SpacialElement;
                            hydratedElement.MoveTo(item.X, item.Y);
                            hydratedElement.ResizeTo(item.Width, item.Height);
                        }
                        yield return function;
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
