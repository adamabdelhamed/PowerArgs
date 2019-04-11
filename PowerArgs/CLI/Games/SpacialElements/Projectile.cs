using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{
    public class Projectile : SpacialElement
    {
        public ConsoleString Pen { get; set; } = new ConsoleString("*", ConsoleColor.Red);
        public float Accelleration { get; set; } = 40;
        public float Range { get; set; } = -1;
        public float angle { get; private set; }
        public SpeedTracker Speed { get; private set; }
        public bool PlaySoundOnImpact { get; set; }

        private IRectangularF startLocation;
        private Force force;
        private Projectile()
        {
            this.ResizeTo(1, 1);
            this.Tags.Add(Weapon.WeaponTag);
            Speed = new SpeedTracker(this);
            Speed.Governor.Rate = TimeSpan.FromSeconds(0);
            Speed.ImpactOccurred.SubscribeForLifetime(Speed_ImpactOccurred, this.Lifetime);
        }

        public Projectile(float x, float y, float angle) : this()
        {
            this.MoveTo(x, y);
            this.angle = angle;
            startLocation = this.Bounds;
            // todo - replace with bullet speed from config
            force = new Force(Speed, Accelleration.NormalizeQuantity(angle), angle);
        }

        public Projectile(float x, float y, IRectangularF target) : this()
        {
            this.MoveTo(x, y);
            this.angle = this.CalculateAngleTo(target);
            startLocation = this.Bounds;
            // todo - replace with bullet speed from config
            force = new Force(Speed, Accelleration.NormalizeQuantity(angle), angle);
        }


        public override void Evaluate()
        {
            if (Range > 0 && this.CalculateDistanceTo(startLocation) > Range)
            {
                this.Lifetime.Dispose();
            }
        }

        private void Speed_ImpactOccurred(Impact impact)
        {
            if (PlaySoundOnImpact)
            {
                Sound.Play("bulletHit");
            }

            DamageBroker.Instance.ReportImpact(impact);
            this.Lifetime.Dispose();
        }
    }

    [SpacialElementBinding(typeof(Projectile))]
    public class ProjectileRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context) => context.DrawString((Element as Projectile).Pen, 0, 0);
    }
}
