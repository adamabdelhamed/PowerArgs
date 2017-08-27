using System;

namespace PowerArgs.Cli.Physics
{
    public class Seeker : SpacialElementFunction
    {
        public bool IsSeeking { get; set; }
        public SpacialElement Seekee { get; private set; }
        public SpeedTracker SeekerSpeed { get; private set; }
        private Force currentForce;
        private float accelleration;

        public bool RemoveWhenReached { get; set; }

        public Seeker(SpacialElement seeker, SpacialElement seekee, SpeedTracker seekerSpeed, float accelleration) : base(seeker)
        {
            this.Seekee = seekee;
            this.SeekerSpeed = seekerSpeed;
            this.accelleration = accelleration;
            Governor.Rate = TimeSpan.FromSeconds(.1);
            Seekee.Lifetime.LifetimeManager.Manage(() => { this.Lifetime.Dispose(); });
        }

        public override void Initialize()
        {
            if (IsSeeking)
            {
                currentForce = new Force(SeekerSpeed, accelleration, Element.Center().CalculateAngleTo(Seekee.Center()));
            }
        }

        public override void Evaluate()
        {
            if (currentForce != null)
            {
                new Force(SeekerSpeed, 1, SpaceExtensions.GetOppositeAngle(currentForce.Angle));
                currentForce = null;
            }

            if (IsSeeking && Element.CalculateDistanceTo(Seekee) < 1)
            {
                Element.MoveTo(Seekee.CenterX, Seekee.CenterY);
                if(RemoveWhenReached)
                {
                    this.Lifetime.Dispose();
                }
            }
            else if (IsSeeking)
            {
                currentForce = new Force(SeekerSpeed, accelleration, Element.Center().CalculateAngleTo(Seekee.Center()));
            }
        }
    }
}
