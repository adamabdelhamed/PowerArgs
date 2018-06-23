using System;

namespace ConsoleGames.Shooter
{
    public class LooseAmmo<TWeaponType> : LooseItem where TWeaponType : Weapon
    {
        private Type weaponType;
        private int amount;
        public LooseAmmo(int amount)
        {
            this.weaponType = typeof(TWeaponType);
            this.amount = amount;
        }

        public override bool CanIncorporate(Character target)
        {
            if (target.Inventory == null) return false;

            foreach (var item in target.Inventory.Items)
            {
                if (item.GetType() == weaponType)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Incorporate(Character target)
        {
            foreach(var item in target.Inventory.Items)
            {
                if(item.GetType() == weaponType)
                {
                    (item as Weapon).AmmoAmount += amount;
                }
            }
        }
    }
}
