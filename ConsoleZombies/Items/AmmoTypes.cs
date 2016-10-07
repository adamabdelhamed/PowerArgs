using PowerArgs;
using System;

namespace ConsoleZombies
{
    public class PistolAmmo : Ammo
    {
        public override ConsoleCharacter Symbol { get { return new ConsoleCharacter('*', ConsoleColor.DarkGray); } }

        public override void IncorporateInto(Inventory inventory)
        {
            inventory.Gun.AmmoAmount += Amount;
        }
    }

    public class RPGAmmo : Ammo
    {
        public override ConsoleCharacter Symbol { get { return new ConsoleCharacter('G', ConsoleColor.DarkGray); } }

        public override void IncorporateInto(Inventory inventory)
        {
            inventory.RPGLauncher.AmmoAmount += Amount;
        }
    }
}
