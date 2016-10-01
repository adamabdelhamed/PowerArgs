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
        public Location Target { get; private set; }

        private SpeedTracker speed;

        public Bullet(Location target)
        {
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(MainCharacter.Current.Bounds.Location.X, MainCharacter.Current.Bounds.Location.Y, 1, 1);
            this.Target = target;
        }

        public override void InitializeThing(Realm r)
        {
            speed = new SpeedTracker(this);
            speed.HitDetectionTypes.Add(typeof(Wall));
            speed.HitDetectionTypes.Add(typeof(Zombie));
            speed.ImpactOccurred += Speed_ImpactOccurred;
            // todo - replace with bullet speed from config
            new Force(speed, 50, this.Bounds.Location.CalculateAngleTo(Target));

        }

        private void Speed_ImpactOccurred(float angle, PowerArgs.Cli.Physics.Rectangle bounds, Thing thingHit)
        {
            if(thingHit is Zombie)
            {
                Realm.Remove(thingHit);
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
