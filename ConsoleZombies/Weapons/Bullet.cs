using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace ConsoleZombies
{
    public class Bullet : Thing
    {

        public float Range { get; set; } = -1;
        public float HealthPoints { get; set; }
        public float angle { get; private set; }

        public SpeedTracker Speed { get; private set; }
        private Location startLocation;

        public Bullet()
        {
            Speed = new SpeedTracker(this);
            Speed.HitDetectionTypes.Add(typeof(Wall));
            Speed.HitDetectionTypes.Add(typeof(Zombie));
            Speed.HitDetectionTypes.Add(typeof(MainCharacter));
            Speed.ImpactOccurred += Speed_ImpactOccurred;
        }

        public Bullet(Location target) : this()
        {
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(MainCharacter.Current.Bounds.Location.X, MainCharacter.Current.Bounds.Location.Y, 1, 1);
            this.angle = this.Bounds.Location.CalculateAngleTo(target);
            this.HealthPoints = 1;
        }

        public Bullet(Location startLocation, float angle) : this()
        {
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(startLocation.X, startLocation.Y, 1, 1);
            this.angle = angle;
            this.HealthPoints = 1;
        }

        public override void InitializeThing(Realm r)
        {
            startLocation = this.Bounds.Location;
            // todo - replace with bullet speed from config
            new Force(Speed, 20, angle);

        }

        public override void Behave(Realm r)
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

        private void Speed_ImpactOccurred(float angle, PowerArgs.Cli.Physics.Rectangle bounds, PowerArgs.Cli.Physics.Thing thingHit)
        {
            if (thingHit is IDestructible)
            {
                var destructible = thingHit as IDestructible;

                destructible.HealthPoints -= this.HealthPoints;
                if (destructible.HealthPoints <= 0)
                {
                    if(thingHit is MainCharacter)
                    {
                        MainCharacter.Current.EatenByZombie.Fire();
                    }
                    Realm.Remove(thingHit);
                }
            }

            Realm.Remove(this);
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
            context.Pen = new PowerArgs.ConsoleCharacter('*', ConsoleColor.Red, ConsoleColor.Gray);
            context.DrawPoint(0, 0);
        }
    }
}
