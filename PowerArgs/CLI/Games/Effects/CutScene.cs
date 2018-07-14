using PowerArgs.Cli.Physics;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public interface ICutScene
    {
        void Start();
    }

    public class CutSceneReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
           if(item.HasValueTag("cutscene") ==false)
           {
                hydratedElement = null;
                return false;
           }

            var scene = SceneFactory.CreateInstance<ICutScene>(item.GetTagValue("cutscene"));
            hydratedElement = TimeFunction.Create(null, init: scene.Start);
            return true;
        }
    }
}
