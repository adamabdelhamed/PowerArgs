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
          

            if (IsRoaming)
            {
                currentForce = new Force(RoamerSpeed, accelleration, NextAngle());
            }

            this.Added.SubscribeOnce(async () =>
            {
                while (this.Lifetime.IsExpired == false)
                {
                    Evaluate();
                    await Time.CurrentTime.DelayAsync(100);
                }
            });
        }

        private float NextAngle()
        {
            var baseVal = ((float)Time.CurrentTime.Now.TotalMilliseconds * Element.Bounds.Left* Element.Bounds.Top) % 359f;

            return baseVal;
        }

        private void Evaluate()
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
