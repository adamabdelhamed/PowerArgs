using System;

namespace PowerArgs.Cli.Physics
{
    public class Force : SpacialElementFunction
    {
        public float Accelleration { get; set; }
        public float Angle { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsPermanentForce { get; set; }
        SpeedTracker tracker;

        public Force(SpeedTracker tracker, float accelleration, float angle, TimeSpan? duration = null) : base(tracker.Element)
        {
            this.Accelleration = accelleration;
            this.Angle = angle;
            this.tracker = tracker;
            this.Duration = duration.HasValue ? duration.Value : TimeSpan.Zero;
        }

        public override void Initialize()
        {
            if (Duration < TimeSpan.Zero)
            {
                this.IsPermanentForce = true;
            }
            else
            {
                this.EndTime = Time.CurrentTime.Now + Duration;
            }

            if (Duration == TimeSpan.Zero)
            {
                float dx, dy;
                CalculateSpeedDeltas(Accelleration, out dx, out dy);
                tracker.SpeedX += dx;
                tracker.SpeedY += dy;
                this.Lifetime.Dispose();
            }
        }

        public override void Evaluate()
        {
            if (!IsPermanentForce && Time.CurrentTime.Now >= EndTime)
            {
                this.Lifetime.Dispose();
                return;
            }

            float dt = (float)(Time.CurrentTime.Now.TotalSeconds - Governor.Rate.TotalSeconds);
            float dSpeed = (Accelleration * dt);
            float dx, dy;

            CalculateSpeedDeltas(dSpeed, out dx, out dy);

            tracker.SpeedX += dx;
            tracker.SpeedY += dy;
        }

        private void CalculateSpeedDeltas(float dSpeed, out float dx, out float dy)
        {
            SpeedTracker.FindEdgesGivenHyp(dSpeed, Angle, out dx, out dy);
        }
    }
}
