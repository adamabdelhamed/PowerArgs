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

        public Weapon CurrentWeapon { get; private set; }

        public Inventory()
        {
            Items.Add(new Pistol());
            CurrentWeapon = Items.First() as Weapon;
        }
    }
}
