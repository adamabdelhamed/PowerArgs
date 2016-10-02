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
        public Weapon RPGLauncher { get; private set; }
        public Inventory()
        {
            Gun = new Pistol();
            RemoteMineDropper = new RemoteMineDropper();
            TimedMineDropper = new TimedMineDropper();
            RPGLauncher = new RPGLauncher();
        }
    }
}
