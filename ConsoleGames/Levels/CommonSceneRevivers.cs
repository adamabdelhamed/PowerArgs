using PowerArgs;
using PowerArgs.Cli.Physics;
using System.Collections.Generic;

namespace ConsoleGames
{
    public class WallReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out SpacialElement hydratedElement)
        {
            hydratedElement = new Wall() { Pen = new ConsoleCharacter(item.Symbol, item.FG, item.BG) };
            return true;
        }
    }
}
