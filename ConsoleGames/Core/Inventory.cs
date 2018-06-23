using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System.Linq;

namespace ConsoleGames
{
    public interface IInventoryItem { }

    public class Inventory : ObservableObject
    {
        public ObservableCollection<IInventoryItem> Items { get; private set; } = new ObservableCollection<IInventoryItem>();
    }
}
