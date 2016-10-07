using System;

namespace PowerArgs.Cli.Physics
{
    public class Seeker : ThingInteraction
    {
        public bool IsSeeking { get; set; }
        public Thing Seekee { get; private set; }
        public SpeedTracker SeekerSpeed { get; private set; }
        private Force currentForce;
        private float accelleration;

        public bool RemoveWhenReached { get; set; }

        public Seeker(Thing seeker, Thing seekee, SpeedTracker seekerSpeed, float accelleration) : base(seeker)
        {
            this.Seekee = seekee;
            this.SeekerSpeed = seekerSpeed;
            this.accelleration = accelleration;
            Governor.Rate = TimeSpan.FromSeconds(.1);
            Seekee.Removed.SubscribeForLifetime(() => { Scene.Remove(this); }, this.LifetimeManager);
        }

        public override void Initialize(Scene scene)
        {
            if (IsSeeking)
            {
                currentForce = new Force(SeekerSpeed, accelleration, MyThing.Bounds.Location.CalculateAngleTo(Seekee.Bounds.Location));
            }
        }

        public override void Behave(Scene scene)
        {
            if (currentForce != null)
            {
                new Force(SeekerSpeed, 1, SceneHelpers.GetOppositeAngle(currentForce.Angle));
                currentForce = null;
            }

            if (IsSeeking && MyThing.Bounds.Location.CalculateDistanceTo(Seekee.Bounds.Location) < 1)
            {
                MyThing.Bounds.MoveTo(Seekee.Bounds.Location);
                if(RemoveWhenReached)
                {
                    Scene.Remove(this);
                }
            }
            else if (IsSeeking)
            {
                currentForce = new Force(SeekerSpeed, accelleration, MyThing.Bounds.Location.CalculateAngleTo(Seekee.Bounds.Location));
            }
        }

        
    }
}
