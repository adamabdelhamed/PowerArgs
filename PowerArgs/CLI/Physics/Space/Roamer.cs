using System;

namespace PowerArgs.Cli.Physics
{
    public class Roamer : SpacialElementFunction
    {
        public bool IsRoaming { get; set; }
        public SpeedTracker RoamerSpeed { get; private set; }
        private Force currentForce;
        private float accelleration;

        public Roamer(SpacialElement roamer, SpeedTracker roamerSpeed, float accelleration) : base(roamer)
        {
            this.RoamerSpeed = roamerSpeed;
            this.accelleration = accelleration;
            Governor.Rate = TimeSpan.FromSeconds(.1);
         }

        public override void Initialize()
        {
            if (IsRoaming)
            {
                currentForce = new Force(RoamerSpeed, accelleration, NextAngle());
            }
        }

        private float NextAngle()
        {
            var baseVal = ((float)Time.CurrentTime.Now.TotalMilliseconds * Element.Bounds.Left* Element.Bounds.Top) % 359f;

            return baseVal;
        }

        public override void Evaluate()
        {
            if (currentForce != null)
            {
                new Force(RoamerSpeed, 1, SpaceExtensions.GetOppositeAngle(currentForce.Angle));
                currentForce = null;
            }

            if (IsRoaming)
            {
                currentForce = new Force(RoamerSpeed, accelleration, NextAngle());
            }
        }     
    }
}
