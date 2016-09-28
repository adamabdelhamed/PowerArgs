using System;

namespace PowerArgs.Cli.Physics
{
    public class Force : ThingInteraction
    {
        public float Accelleration { get; set; }
        public float Angle { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsPermanentForce { get; set; }
        SpeedTracker tracker;

        public Force(SpeedTracker tracker, float accelleration, float angle, TimeSpan? duration = null) : base(tracker.MyThing)
        {
            this.Accelleration = accelleration;
            this.Angle = angle;
            this.tracker = tracker;
            this.Duration = duration.HasValue ? duration.Value : TimeSpan.Zero;
        }

        public override void Initialize(Realm realm)
        {
            base.Initialize(realm);
            if (Duration < TimeSpan.Zero)
            {
                this.IsPermanentForce = true;
            }
            else
            {
                this.EndTime = realm.ElapsedTime + Duration;
            }

            if (Duration == TimeSpan.Zero)
            {
                float dx, dy;
                CalculateSpeedDeltas(Accelleration, out dx, out dy);
                tracker.SpeedX += dx;
                tracker.SpeedY += dy;
                realm.Remove(this);
            }
        }

        public override void Behave(Realm realm)
        {
            if (!IsPermanentForce && realm.ElapsedTime >= EndTime)
            {
                realm.Remove(this);
                return;
            }

            float dt = (float)(realm.ElapsedTime.TotalSeconds - LastBehavior.TotalSeconds);
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
