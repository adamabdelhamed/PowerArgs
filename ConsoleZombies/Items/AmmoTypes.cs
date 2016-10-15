using PowerArgs;
using System;

namespace ConsoleZombies
{
    [AmmoInfo("Pistol")]
    public class PistolAmmo : Ammo
    {
        public override ConsoleCharacter Symbol { get { return new ConsoleCharacter('*', ConsoleColor.DarkGray); } }

        public override void IncorporateInto(Inventory inventory)
        {
            Pistol pistol;
            if (inventory.TryGet<Pistol>(out pistol) == false)
            {
                pistol = new Pistol() { AmmoAmount = 0 };
                inventory.AvailableWeapons.Add(pistol);
                if(inventory.PrimaryWeapon == null)
                {
                    inventory.PrimaryWeapon = pistol;
                }
            }

            pistol.AmmoAmount += Amount;
        }
    }

    [AmmoInfo("RPG - Rocket propelled grenade")]
    public class RPGAmmo : Ammo
    {
        public override ConsoleCharacter Symbol { get { return new ConsoleCharacter('G', ConsoleColor.DarkGray); } }

        public override void IncorporateInto(Inventory inventory)
        {
            RPGLauncher launcher;
            if (inventory.TryGet<RPGLauncher>(out launcher) == false)
            {
                launcher = new RPGLauncher() { AmmoAmount = 0 };
                inventory.AvailableWeapons.Add(launcher);
                if (inventory.ExplosiveWeapon == null) inventory.ExplosiveWeapon = launcher;
            }

            launcher.AmmoAmount += Amount;
        }
    }
}
