using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class Bullet : Thing
    {
        public float Range { get; set; } = -1;
        public float HealthPoints { get; set; }
        public float angle { get; private set; }
        public SpeedTracker Speed { get; private set; }
        public bool PlaySoundOnImpact { get; set; }

        private Location startLocation;

        public Bullet()
        {
            Speed = new SpeedTracker(this);
            Speed.HitDetectionTypes.Add(typeof(Wall));
            Speed.HitDetectionTypes.Add(typeof(Zombie));
            Speed.HitDetectionTypes.Add(typeof(MainCharacter));
            Speed.Governor.Rate = TimeSpan.FromSeconds(0);
            Speed.ImpactOccurred.SubscribeForLifetime(Speed_ImpactOccurred, this.LifetimeManager);
        }

        public Bullet(Location target) : this()
        {
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(MainCharacter.Current.Bounds.Location.X, MainCharacter.Current.Bounds.Location.Y, 1, 1);
            this.Bounds.Pad(.1f);
            this.angle = this.Bounds.Location.CalculateAngleTo(target);
            this.HealthPoints = 1;
        }

        public Bullet(Location startLocation, float angle) : this()
        {
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(startLocation.X, startLocation.Y, 1, 1);
            this.Bounds.Pad(.25f);
            this.angle = angle;
            this.HealthPoints = 1;
        }

        public override void InitializeThing(Scene r)
        {
            startLocation = this.Bounds.Location;
            // todo - replace with bullet speed from config
            new Force(Speed, 35, angle);

        }

        public override void Behave(Scene r)
        {
            if(Range > 0 && this.Bounds.Location.CalculateDistanceTo(startLocation) > Range)
            {
                r.Remove(this);
            }
            else if(Speed.Speed < 5)
            {
                r.Remove(this);
            }
        }

        private void Speed_ImpactOccurred(Impact impact)
        {
            if (impact.ThingHit is IDestructible)
            {
                if (PlaySoundOnImpact)
                {
                    SoundEffects.Instance.PlaySound("bulletHit");
                }
                var destructible = impact.ThingHit as IDestructible;

                destructible.TakeDamage(this.HealthPoints);
            }

            Scene.Remove(this);
        }
    }

    [ThingBinding(typeof(Bullet))]
    public class BulletRenderer : ThingRenderer
    {
        public BulletRenderer()
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
