using PowerArgs;
using System;

namespace ConsoleZombies
{
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
            }

            pistol.AmmoAmount += Amount;
        }
    }

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
            }

            launcher.AmmoAmount += Amount;
        }
    }
}
