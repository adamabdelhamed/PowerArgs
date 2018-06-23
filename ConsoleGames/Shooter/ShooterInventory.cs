using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleGames.Shooter
{
    public class ShooterInventory : Inventory
    {
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

        public ShooterInventory()
        {
            Items.Added.SubscribeForLifetime((item) =>
            {
                if(item is Weapon)
                {
                    var weapon = item as Weapon;

                    if(weapon.Style == WeaponStyle.Primary)
                    {
                        if (PrimaryWeapon == null || PrimaryWeapon.AmmoAmount == 0)
                        {
                            PrimaryWeapon = weapon;
                        }
                    }
                    else
                    {
                        if(ExplosiveWeapon == null || ExplosiveWeapon.AmmoAmount == 0)
                        {
                            ExplosiveWeapon = weapon;
                        }
                    }
                }

 
            }, Lifetime.Forever);
        }
    }
}
