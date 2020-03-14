using PowerArgs.Cli.Physics;
using System;
using System.Linq;
namespace PowerArgs.Games
{
    public enum WeaponStyle
    {
        Primary,
        Explosive,
        Shield
    }

    public class WeaponElement : SpacialElement
    {
        public Weapon Weapon { get; set; }
        public WeaponElement(Weapon w)
        {
            this.Weapon = w;
            if(w?.Holder != null)
            {
                this.MoveTo(0, 0, w.Holder.ZIndex);
            }
        }
    }

    public class NoOpWeapon : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;

        public override void FireInternal(bool alt)
        {
             
        }
    }

    public abstract class Weapon : ObservableObject, IInventoryItem
    {
        public virtual float ProjectileSpeedHint => 50;

        public bool AllowMultiple => false;
        public static Event<Weapon> OnFireEmpty { get; private set; } = new Event<Weapon>();
        public static Event<Weapon> OnFire { get; private set; } = new Event<Weapon>();
        public Event<WeaponElement> OnWeaponElementEmitted { get; private set; } = new Event<WeaponElement>();
        public SmartTrigger Trigger { get; set; }
        public const string WeaponTag = "Weapon";
        public Character Holder { get; set; }
        public object Tag { get; set; }
        public abstract WeaponStyle Style { get; }
        public virtual float Strength { get; set; }
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



        private TimeSpan lastFireTime = TimeSpan.Zero;

        public void TryFire(bool alt)
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
                    FireInternal(alt);
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

        public abstract void FireInternal(bool alt);
    }
}
