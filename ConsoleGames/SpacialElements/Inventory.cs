using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System.Linq;

namespace ConsoleGames
{
    public interface IInventoryItem { }

    public class Inventory : ObservableObject
    {
        public ObservableCollection<IInventoryItem> Items { get; private set; } = new ObservableCollection<IInventoryItem>();

        public Character Owner { get; private set; }

        private Weapon _primaryWeapon;
        public Weapon PrimaryWeapon
        {
            get
            {
                return _primaryWeapon;
            }
            set
            {
                if (Items.Contains(value) == false)
                {
                    Items.Add(value);
                }

                _primaryWeapon = value;
                FirePropertyChanged(nameof(PrimaryWeapon));
            }
        }

        private Weapon _explosiveWeapon;
        public Weapon ExplosiveWeapon
        {
            get
            {
                return _explosiveWeapon;
            }
            set
            {
                if (Items.Contains(value) == false)
                {
                    Items.Add(value);
                }

                _explosiveWeapon = value;
                FirePropertyChanged(nameof(ExplosiveWeapon));
            }
        }

        public Inventory(Character owner)
        {
            this.Owner = owner;
            Items.Added.SubscribeForLifetime((item) =>
            {
                if (item is Weapon)
                {
                    var weapon = item as Weapon;
                    weapon.Holder = this.Owner;
                    if (weapon.Style == WeaponStyle.Primary)
                    {
                        if (PrimaryWeapon == null || PrimaryWeapon.AmmoAmount == 0)
                        {
                            PrimaryWeapon = weapon;
                        }
                    }
                    else
                    {
                        if (ExplosiveWeapon == null || ExplosiveWeapon.AmmoAmount == 0)
                        {
                            ExplosiveWeapon = weapon;
                        }
                    }
                }


            }, Lifetime.Forever);
        }
    }
}
