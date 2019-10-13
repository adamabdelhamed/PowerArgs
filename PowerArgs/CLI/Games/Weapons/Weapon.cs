using PowerArgs.Cli.Physics;
using System.Linq;
namespace PowerArgs.Games
{
    public enum WeaponStyle
    {
        Primary,
        Explosive
    }

    public abstract class Weapon : ObservableObject, IInventoryItem
    {
        public static Event<Weapon> OnFireEmpty { get; private set; } = new Event<Weapon>();
        public static Event<Weapon> OnFire { get; private set; } = new Event<Weapon>();

        public const string WeaponTag = "Weapon";
        public Character Holder { get; set; }

        public abstract WeaponStyle Style { get; }

        public ConsoleString DisplayName { get; set; }

        public int AmmoAmount
        {
            get { return Get<int>(); } set { Set(value); }
        }

        /// <summary>
        /// If a weapon is picked up and it's the highest ranking in the inventory then it will automatically be put into use
        /// </summary>
        public int PowerRanking { get; set; }

        public Weapon()
        {
            DisplayName = GetType().Name.ToConsoleString();
        }

        public float CalculateAngleToTarget()
        {
            var angle = Holder.Target != null ?
                Holder.CalculateAngleTo(Holder.Target) :
                Holder.Speed.Angle;

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
                OnFire.Fire(this);
                FireInternal();
                if (AmmoAmount > 0)
                {
                    AmmoAmount--;

                    if(AmmoAmount == 0)
                    {
                        var alternative = Holder.Inventory.Items
                            .WhereAs<Weapon>()
                            .Where(w => w.Style == this.Style && w.AmmoAmount > 0)
                            .OrderByDescending(w => w.PowerRanking)
                            .FirstOrDefault();

                        if(alternative != null)
                        {
                            if(alternative.Style == WeaponStyle.Primary)
                            {
                                Holder.Inventory.PrimaryWeapon = alternative;
                            }
                            else
                            {
                                Holder.Inventory.ExplosiveWeapon = alternative;
                            }
                        }
                    }

                }
            }
            else
            {
                OnFireEmpty.Fire(this);
            }
        }

        public abstract void FireInternal();
    }
}
