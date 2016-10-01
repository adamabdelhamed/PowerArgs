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
        public ObservableCollection<Item> Items { get; private set; } = new ObservableCollection<Item>();

        public Weapon Gun { get; private set; }
        public Weapon RemoteMineDropper { get; private set; }
        public Weapon TimedMineDropper { get; private set; }
        public Weapon GrenadeThrower { get; private set; }
        public Inventory()
        {
            Gun = new Pistol();
            RemoteMineDropper = new RemoteMineDropper();
            TimedMineDropper = new TimedMineDropper();
            GrenadeThrower = new GrenadeThrower();
        }
    }
}
