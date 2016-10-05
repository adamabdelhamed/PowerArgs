using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public abstract class Weapon : InventoryItem
    {
        public int AmmoAmount { get; set; }

        public bool TryFire()
        {
            if(AmmoAmount > 0)
            {
                FireInternal();
                AmmoAmount--;
                return true;
            }
            else
            {
                return false;
            }
        }

        public abstract void FireInternal();
    }
}
