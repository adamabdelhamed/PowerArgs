using PowerArgs;
using PowerArgs.Cli.Physics;

namespace ConsoleGames
{
    public class WallReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, out SpacialElement hydratedElement)
        {
            hydratedElement = new Wall() { Pen = new ConsoleCharacter(item.Symbol, item.FG, item.BG) };
            return true;
        }
    }
}
