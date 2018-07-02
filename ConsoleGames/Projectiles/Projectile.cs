using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public class Projectile : SpacialElement
    {
        public float Range { get; set; } = -1;
        public float HealthPoints { get; set; }
        public float angle { get; private set; }
        public SpeedTracker Speed { get; private set; }
        public bool PlaySoundOnImpact { get; set; }

        private IRectangular startLocation;
        private Force force;
        public Projectile()
        {
            this.ResizeTo(1, 1);
            Speed = new SpeedTracker(this);
            Speed.HitDetectionTypes.Add(typeof(Enemy));
            Speed.HitDetectionTypes.Add(typeof(Wall));
            Speed.Governor.Rate = TimeSpan.FromSeconds(0);
            Speed.ImpactOccurred.SubscribeForLifetime(Speed_ImpactOccurred, this.Lifetime.LifetimeManager);
        }

        public Projectile(float x, float y, float angle) : this()
        {
            this.MoveTo(x, y);
            this.angle = angle;
            this.HealthPoints = 1;
        }

        public Projectile(float x, float y, IRectangular target) : this()
        {
            this.MoveTo(x, y);
            this.angle = this.CalculateAngleTo(target);
            this.HealthPoints = 1;
        }


        public override void Initialize()
        {
            startLocation = this.Bounds;
            // todo - replace with bullet speed from config
            force = new Force(Speed, 35, angle);
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
            if (impact.ElementHit is IDestructible)
            {
                if (PlaySoundOnImpact)
                {
                    // todo - uncomment
                   // SoundEffects.Instance.PlaySound("bulletHit");
                }
                var destructible = impact.ElementHit as IDestructible;

                destructible.TakeDamage(this.HealthPoints);
            }

            this.Lifetime.Dispose();
        }
    }

    [SpacialElementBinding(typeof(Projectile))]
    public class ProjectileRenderer : SpacialElementRenderer
    {
        public ProjectileRenderer()
        {
            TransparentBackground = true;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new PowerArgs.ConsoleCharacter('*', ConsoleColor.Red);
            context.DrawPoint(0, 0);
        }
    }
}
