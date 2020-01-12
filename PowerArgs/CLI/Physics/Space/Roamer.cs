using System;

namespace PowerArgs.Cli.Physics
{
    public class Roamer : SpacialElementFunction
    {
        public bool IsRoaming { get; set; }
        public Velocity RoamerSpeed { get; private set; }
        private Force currentForce;
        private float accelleration;

        public Roamer(SpacialElement roamer, Velocity roamerSpeed, float accelleration) : base(roamer)
        {
            this.RoamerSpeed = roamerSpeed;
            this.accelleration = accelleration;
            Governor.Rate = TimeSpan.FromSeconds(.1);

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
                new Force(RoamerSpeed, 1,  currentForce.Angle.GetOppositeAngle());
                currentForce = null;
            }

            if (IsRoaming)
            {
                currentForce = new Force(RoamerSpeed, accelleration, NextAngle());
            }
        }     
    }
}
