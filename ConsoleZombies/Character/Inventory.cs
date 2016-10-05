using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class Inventory
    {
        public Weapon Gun { get; private set; }
        public Weapon RemoteMineDropper { get; private set; }
        public Weapon TimedMineDropper { get; private set; }
        public Weapon RPGLauncher { get; private set; }
        public Inventory()
        {
            Gun = new Pistol() { AmmoAmount = 10 };
            RemoteMineDropper = new RemoteMineDropper() { AmmoAmount = 1 } ;
            TimedMineDropper = new TimedMineDropper() { AmmoAmount = 1 };
            RPGLauncher = new RPGLauncher() { AmmoAmount = 1 };
        }

        public void Add(InventoryItem item)
        {
            item.IncorporateInto(this);
        }
    }
}
