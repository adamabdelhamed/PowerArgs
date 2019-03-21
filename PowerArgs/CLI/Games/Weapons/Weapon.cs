using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public enum WeaponStyle
    {
        Primary,
        Explosive
    }

    public abstract class Weapon : ObservableObject, IInventoryItem
    {
        public List<Type> HitDetectionTypes { get; set; } = new List<Type>();
        public Character Holder { get; set; }

        public abstract WeaponStyle Style { get; }

        public int AmmoAmount
        {
            get { return Get<int>(); } set { Set(value); }
        }

        public float CalculateAngleToTarget()
        {
            var angle = Holder.Target != null ?
                Holder.CalculateAngleTo(Holder.Target) :
                MainCharacter.Current.Speed.Angle;

            if (Holder == MainCharacter.Current && MainCharacter.Current.FreeAimCursor != null)
            {
                angle = Holder.CalculateAngleTo(MainCharacter.Current.FreeAimCursor);
            };

            return angle;
        }

        public void TryFire()
        {
            if ((AmmoAmount > 0 || AmmoAmount == -1) && Holder != null)
            {
                FireInternal();
                if (AmmoAmount > 0)
                {
                    AmmoAmount--;
                }
            }
        }

        public abstract void FireInternal();
    }
}
