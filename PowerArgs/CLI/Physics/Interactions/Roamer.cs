using System;

namespace PowerArgs.Cli.Physics
{
    public class Roamer : ThingInteraction
    {
        public bool IsRoaming { get; set; }
        public SpeedTracker RoamerSpeed { get; private set; }
        private Force currentForce;
        private float accelleration;

        public Roamer(Thing roamer, SpeedTracker roamerSpeed, float accelleration) : base(roamer)
        {
            this.RoamerSpeed = roamerSpeed;
            this.accelleration = accelleration;
            Governor.Rate = TimeSpan.FromSeconds(.1);
         }

        public override void Initialize(Scene scene)
        {
            if (IsRoaming)
            {
                currentForce = new Force(RoamerSpeed, accelleration, NextAngle());
            }
        }

        private float NextAngle()
        {
            var baseVal = ((float)Scene.ElapsedTime.TotalMilliseconds * MyThing.Bounds.X* MyThing.Bounds.Y) % 359f;

            return baseVal;
        }

        public override void Behave(Scene scene)
        {
            if (currentForce != null)
            {
                new Force(RoamerSpeed, 1, SceneHelpers.GetOppositeAngle(currentForce.Angle));
                currentForce = null;
            }

            if (IsRoaming)
            {
                currentForce = new Force(RoamerSpeed, accelleration, NextAngle());
            }
        }     
    }
}
