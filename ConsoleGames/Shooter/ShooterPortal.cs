using System;
using System.Collections.Generic;
using System.Text;
using PowerArgs.Cli.Physics;

namespace ConsoleGames.Shooter
{
    public class ShooterPortal : Portal
    {
        public string Destination { get; set; }

        public ShooterPortal()
        {
            this.TouchedByCharacter.SubscribeForLifetime(OnTouchedByCharacter, Lifetime.LifetimeManager);
        }

        private void OnTouchedByCharacter(Character c)
        {
            if (c == MainCharacter.Current)
            {
                var level = LevelEditor.LoadBySimpleName(Destination);
                GameApp.Load(level);
            }
        }
    }

    public class ShooterPortalReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, out SpacialElement hydratedElement)
        {
            if(item.HasValueTag("destination") == false)
            {
                hydratedElement = null;
                return false;
            }

            hydratedElement = new ShooterPortal() { Destination = item.GetTagValue("destination") };
            return true;

        }
    }
}
