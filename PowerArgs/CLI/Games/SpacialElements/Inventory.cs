using System.Linq;

namespace PowerArgs.Games
{
    public interface IInventoryItem
    {
        ConsoleString DisplayName { get; }
        Character Holder { get; set; }

        bool AllowMultiple { get; }
    }

    public class Inventory : ObservableObject
    {
        private Lifetime itemsLifetime;
        public ObservableCollection<IInventoryItem> Items { get => Get<ObservableCollection<IInventoryItem>>(); private set => Set(value); }  

        public Character Owner { get => Get<Character>(); set => Set(value); }

        private Weapon _primaryWeapon;

        public Weapon PrimaryWeapon
        {
            get
            {
                return _primaryWeapon;
            }
            set
            {
                if (value != null && Items.Contains(value) == false)
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
                if (value != null && Items.Contains(value) == false)
                {
                    Items.Add(value);
                }

                _explosiveWeapon = value;
                FirePropertyChanged(nameof(ExplosiveWeapon));
            }
        }

        private Weapon _shieldWeapon;
        public Weapon ShieldWeapon
        {
            get
            {
                return _shieldWeapon;
            }
            set
            {
                if (Items.Contains(value) == false)
                {
                    Items.Add(value);
                }

                _shieldWeapon = value;
                FirePropertyChanged(nameof(ShieldWeapon));
            }
        }

        public Inventory()
        {
            this.SubscribeForLifetime(nameof(Items), () =>
            {
                itemsLifetime?.Dispose();
                itemsLifetime = new Lifetime();
                Items.ForEach(item => ProcessItem(item));
                Items.Added.SubscribeForLifetime((item) => ProcessItem(item), itemsLifetime);

            }, this);

            this.SubscribeForLifetime(nameof(Owner), () =>
            {
                foreach(var item in Items.Where(i => i is Weapon).Select(i => i as Weapon))
                {
                    item.Holder = Owner;
                }
            }, this);

            Items = new ObservableCollection<IInventoryItem>();
        }

       

        private void ProcessItem(IInventoryItem item)
        {
            item.Holder = this.Owner;
            if (item is Weapon)
            {
                var priorWeapons = Items.WhereAs<Weapon>().Where(w => w != item);

                var priorPrimaryWeapons = priorWeapons.Where(w => w.Style == WeaponStyle.Primary);
                var priorExplosiveWeapons = priorWeapons.Where(w => w.Style == WeaponStyle.Explosive);
                var priorShieldWeapons = priorWeapons.Where(w => w.Style == WeaponStyle.Shield);

                var highestPrimaryWeapon = priorPrimaryWeapons.Any() ? priorPrimaryWeapons.OrderByDescending(w => w.PowerRanking).First() : null;
                var highestExplosiveWeapon = priorExplosiveWeapons.Any() ? priorExplosiveWeapons.OrderByDescending(w => w.PowerRanking).First() : null;
                var highestShieldWeapon = priorShieldWeapons.Any() ? priorShieldWeapons.OrderByDescending(w => w.PowerRanking).First() : null;

                var weapon = item as Weapon;
                weapon.Holder = this.Owner;
                if (weapon.Style == WeaponStyle.Primary)
                {
                    if (PrimaryWeapon == null || PrimaryWeapon.AmmoAmount == 0 || weapon.Strength > highestPrimaryWeapon.Strength)
                    {
                        PrimaryWeapon = weapon;
                    }
                }
                else if(weapon.Style == WeaponStyle.Explosive)
                {
                    if (ExplosiveWeapon == null || ExplosiveWeapon.AmmoAmount == 0 || weapon.Strength > highestExplosiveWeapon.Strength)
                    {
                        ExplosiveWeapon = weapon;
                    }
                }
                else if (weapon.Style == WeaponStyle.Shield)
                {
                    if (highestShieldWeapon == null || weapon.Strength > highestShieldWeapon.Strength)
                    {
                        ShieldWeapon = weapon;
                    }
                }
            }
        }
    }
}
