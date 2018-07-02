using PowerArgs.Cli.Physics;
using System;
using System.Linq;
using System.Reflection;

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

    public class AmmoReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, out SpacialElement hydratedElement)
        {
            var ammoTag = item.Tags.Where(testc => testc.StartsWith("ammo:")).SingleOrDefault();
            var amountTag = item.Tags.Where(testc => testc.StartsWith("amount:")).SingleOrDefault();
            if (ammoTag == null)
            {
                hydratedElement = null;
                return false;
            }

            var weaponTypeName = this.ParseTagValue(ammoTag);
            var weaponType = Type.GetType(weaponTypeName, false, true);

            if(weaponType == null)
            {
                weaponType = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Weapon)) && t.Name == weaponTypeName).SingleOrDefault();
            }

            if (weaponType == null)
            {
                weaponType = Assembly.GetEntryAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Weapon)) && t.Name == weaponTypeName).SingleOrDefault();
            }

            if(weaponType == null)
            {
                throw new ArgumentException("Could not resolve weapon type: "+weaponTypeName);
            }

            var amount = int.Parse(this.ParseTagValue(amountTag));

            var weapon = Activator.CreateInstance(weaponType) as Weapon;
            weapon.AmmoAmount = amount;

            hydratedElement = new LooseWeapon(weapon);
            return true;
        }
    }


    public class EnemyReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, out SpacialElement hydratedElement)
        {
            var enemyTag = item.Tags.Where(testc => testc.Equals("enemy")).SingleOrDefault();
            if (enemyTag == null)
            {
                hydratedElement = null;
                return false;
            }

            hydratedElement = new Enemy();
            return true;
        }
    }
}
