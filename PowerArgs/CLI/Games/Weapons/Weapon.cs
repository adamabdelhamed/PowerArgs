using PowerArgs.Cli.Physics;
using System;
using System.Linq;
namespace PowerArgs.Games
{
    public enum WeaponStyle
    {
        Primary,
        Explosive
    }

    public class WeaponElement : SpacialElement
    {
        public Weapon Weapon { get; set; }
        public WeaponElement(Weapon w)
        {
            this.Weapon = w;
        }
    }

    public abstract class Weapon : ObservableObject, IInventoryItem
    {
        public static Event<Weapon> OnFireEmpty { get; private set; } = new Event<Weapon>();
        public static Event<Weapon> OnFire { get; private set; } = new Event<Weapon>();
        public SmartTrigger Trigger { get; set; }
        public const string WeaponTag = "Weapon";
        public Character Holder { get; set; }
        public object Tag { get; set; }
        public abstract WeaponStyle Style { get; }
        public float Strength { get; set; }
        public ConsoleString DisplayName { get; set; }

        public int AmmoAmount
        {
            get { return Get<int>(); } set { Set(value); }
        }

        protected TimeSpan MinTimeBetweenShots { get; set; } = TimeSpan.FromSeconds(.05);

        /// <summary>
        /// If a weapon is picked up and it's the highest ranking in the inventory then it will automatically be put into use
        /// </summary>
        public int PowerRanking { get; set; }

        public Weapon()
        {
            DisplayName = GetType().Name.ToConsoleString();
            AmmoAmount = -1;
        }

        public float CalculateAngleToTarget()
        {
            var angle = Holder.Target != null ?
                Holder.CalculateAngleTo(Holder.Target.Center()) :
                Holder.Velocity.Angle;

            if (Holder == MainCharacter.Current && MainCharacter.Current.FreeAimCursor != null)
            {
                angle = Holder.CalculateAngleTo(MainCharacter.Current.FreeAimCursor.Center());
            };

            return angle;
        }

        private TimeSpan lastFireTime = TimeSpan.Zero;

        public void TryFire()
        {
            if (Trigger == null || Trigger.AllowFire())
            {
                if ((AmmoAmount > 0 || AmmoAmount == -1) && Holder != null)
                {
                    if (Time.CurrentTime.Now - lastFireTime < MinTimeBetweenShots)
                    {
                        return;
                    }
                    lastFireTime = Time.CurrentTime.Now;

                    OnFire.Fire(this);
                    FireInternal();
                    if (AmmoAmount > 0)
                    {
                        AmmoAmount--;

                        if (AmmoAmount == 0)
                        {
                            var alternative = Holder.Inventory.Items
                                .WhereAs<Weapon>()
                                .Where(w => w.Style == this.Style && w.AmmoAmount > 0)
                                .OrderByDescending(w => w.PowerRanking)
                                .FirstOrDefault();

                            if (alternative != null)
                            {
                                if (alternative.Style == WeaponStyle.Primary)
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
        }

        public abstract void FireInternal();
    }
}
