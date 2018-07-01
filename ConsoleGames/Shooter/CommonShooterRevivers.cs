using PowerArgs.Cli.Physics;

namespace ConsoleGames.Shooter
{
    public class MainCharacterReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, out SpacialElement hydratedElement)
        {
            if (item.Tags.Contains("main-character") == false)
            {
                hydratedElement = null;
                return false;
            }

            hydratedElement = new MainCharacter();
            return true;
        }
    }
}
