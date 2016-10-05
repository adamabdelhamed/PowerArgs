using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public abstract class Item : Thing
    {
        public abstract InventoryItem Convert();
    }

    public class InventoryItem
    {
        public virtual void IncorporateInto(Inventory inventory) { }
    }
}
