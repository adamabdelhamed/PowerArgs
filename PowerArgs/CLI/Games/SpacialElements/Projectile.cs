using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{
    public class Projectile : WeaponElement
    {
        public static Event<Impact> OnAudibleImpact { get; private set; } = new Event<Impact>();

        public const float StandardWidth = 1f;
        public const float StandardHeight = 1f;

        public ConsoleString Pen { get; set; } = new ConsoleString("*", ConsoleColor.Red);
        public float Accelleration { get; set; } = 40;
        public float Range { get; set; } = -1;
        public float angle { get; private set; }
        public Velocity Velocity { get; private set; }
        public bool PlaySoundOnImpact { get; set; }

        private IRectangularF startLocation;
        private Force force;
        private Projectile(Weapon w) : base(w)
        {
            this.ResizeTo(StandardWidth, StandardHeight);
            this.Tags.Add(Weapon.WeaponTag);
            Velocity = new Velocity(this);
            Velocity.Governor.Rate = TimeSpan.FromSeconds(0);
            Velocity.ImpactOccurred.SubscribeForLifetime(Speed_ImpactOccurred, this.Lifetime);
            this.Velocity.HitDetectionExclusionTypes.Add(typeof(Projectile));
            if (w?.Holder != null)
            {
                this.Velocity.HitDetectionExclusions.Add(w.Holder);
            }
            Time.CurrentTime.QueueAction("Init projectile force", () => { force = new Force(Velocity, Accelleration.NormalizeQuantity(angle), angle); });

            
            this.SizeOrPositionChanged.SubscribeForLifetime(() =>
            {
                if (Range > 0 && this.CalculateDistanceTo(startLocation) > Range)
                {
                    this.Lifetime.Dispose();
                }
            }, this.Lifetime);
            

            this.Governor.Rate = TimeSpan.FromSeconds(-1);
        }

        public Projectile(Weapon w,float x, float y, float angle) : this(w)
        {
            this.MoveTo(x, y);
            this.angle = angle;
            startLocation = this.Bounds;
        }

        public Projectile(Weapon w,float x, float y, IRectangularF target) : this(w)
        {
            this.MoveTo(x, y);
            this.angle = this.CalculateAngleTo(target);
            startLocation = this.Bounds;
        }


     

        private void Speed_ImpactOccurred(Impact impact)
        {
            if (PlaySoundOnImpact)
            {
                OnAudibleImpact.Fire(impact);
            }

            DamageBroker.Instance.ReportImpact(impact);
            this.Lifetime.Dispose();
        }
    }

    [SpacialElementBinding(typeof(Projectile))]
    public class ProjectileRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            context.DrawString((Element as Projectile).Pen, 0, 0);
        }
    }
}
