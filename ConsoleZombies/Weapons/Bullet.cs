using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace ConsoleZombies
{
    public class Bullet : PowerArgs.Cli.Physics.Thing
    {

        public float Range { get; set; } = -1;
        public float HealthPoints { get; set; }
        public float angle { get; private set; }

        private SpeedTracker speed;
        private Location startLocation;
        public Bullet(Location target)
        {
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(MainCharacter.Current.Bounds.Location.X, MainCharacter.Current.Bounds.Location.Y, 1, 1);
            this.angle = this.Bounds.Location.CalculateAngleTo(target);
            this.HealthPoints = 1;
        }

        public Bullet(Location startLocation, float angle)
        {
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(startLocation.X, startLocation.Y, 1, 1);
            this.angle = angle;
            this.HealthPoints = 1;
        }

        public override void InitializeThing(Realm r)
        {
            speed = new SpeedTracker(this);
            speed.HitDetectionTypes.Add(typeof(Wall));
            speed.HitDetectionTypes.Add(typeof(Zombie));
            speed.ImpactOccurred += Speed_ImpactOccurred;
            startLocation = this.Bounds.Location;
            // todo - replace with bullet speed from config
            new Force(speed, 20, angle);

        }

        public override void Behave(Realm r)
        {
            if(Range > 0 && this.Bounds.Location.CalculateDistanceTo(startLocation) > Range)
            {
                r.Remove(this);
            }
        }

        private void Speed_ImpactOccurred(float angle, PowerArgs.Cli.Physics.Rectangle bounds, PowerArgs.Cli.Physics.Thing thingHit)
        {
            if (thingHit is Zombie)
            {
                var zombie = thingHit as Zombie;

                zombie.HealthPoints -= this.HealthPoints;
                if (zombie.HealthPoints <= 0)
                {
                    Realm.Remove(zombie);
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
