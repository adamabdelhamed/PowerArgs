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
            this.tracker = tracker ?? throw new ArgumentNullException();
            this.Duration = duration.HasValue ? duration.Value : TimeSpan.Zero;

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
                var end = tracker.Element.MoveTowards(tracker.Angle, tracker.Speed).MoveTowards(angle, accelleration);
                var newAngle = tracker.Element.CalculateAngleTo(end);
                var newSpeed = tracker.Element.CalculateDistanceTo(end);
                tracker.Angle = newAngle;
                tracker.Speed = newSpeed;
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

            var increment = Governor.Rate.TotalSeconds;
            if (increment == 0) increment = Time.CurrentTime.Increment.TotalSeconds;

            float dt = (float)(Time.CurrentTime.Now.TotalSeconds - Governor.Rate.TotalSeconds);
            float dSpeed = (Accelleration * dt);
            var end = tracker.Element.MoveTowards(tracker.Angle, tracker.Speed).MoveTowards(Angle, dSpeed);
            var newAngle = tracker.Element.CalculateAngleTo(end);
            var newSpeed = tracker.Element.CalculateDistanceTo(end);
            tracker.Angle = newAngle;
            tracker.Speed = newSpeed;
        }

    

        private void CalculateSpeedDeltas(float dSpeed, out float dx, out float dy)
        {
            SpeedTracker.FindEdgesGivenHyp(dSpeed, Angle, out dx, out dy);
        }
    }
}
