using System;

namespace PowerArgs.Cli.Physics
{
    public class Seeker : ThingInteraction
    {
        public Thing Seekee { get; private set; }
        public SpeedTracker SeekerSpeed { get; private set; }
        private Force currentForce;
        private float accelleration;
        public Seeker(Thing seeker, Thing seekee, SpeedTracker seekerSpeed, float accelleration) : base(seeker)
        {
            this.Seekee = seekee;
            this.SeekerSpeed = seekerSpeed;
            this.accelleration = accelleration;
            Governor.Rate = TimeSpan.FromSeconds(.1);
            Seekee.Removed.SubscribeForLifetime(() => { Realm.Remove(this); }, this.LifetimeManager);
        }

        public override void Initialize(Realm realm)
        {
            currentForce = new Force(SeekerSpeed, accelleration, MyThing.Bounds.Location.CalculateAngleTo(Seekee.Bounds.Location));
        }

        public override void Behave(Realm realm)
        {
            new Force(SeekerSpeed, 1, Offset(currentForce.Angle));
            currentForce = new Force(SeekerSpeed, accelleration, MyThing.Bounds.Location.CalculateAngleTo(Seekee.Bounds.Location));
        }

        private float Offset(float angle)
        {
            if(angle < 180)
            {
                return angle + 180;
            }
            else
            {
                return angle - 180;
            }
        }
    }
}
