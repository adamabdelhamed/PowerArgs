namespace PowerArgs.Cli.Physics;
public class Force2 : Lifetime
{
    public float Accelleration { get; set; }
    public Angle Angle { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsPermanentForce { get; set; }
    Velocity2 tracker;

    public Force2(Velocity2 tracker, float accelleration, Angle angle, TimeSpan? duration = null)
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
            this.EndTime = DateTime.Now + Duration;
        }

        if (Duration == TimeSpan.Zero)
        {
            var end = tracker.Collider.Bounds.OffsetByAngleAndDistance(tracker.Angle, tracker.Speed).OffsetByAngleAndDistance(angle, accelleration);
            var newAngle = tracker.Collider.Bounds.CalculateAngleTo(end);
            var newSpeed = tracker.Collider.Bounds.CalculateDistanceTo(end);
            tracker.Angle = newAngle;
            tracker.Speed = newSpeed;
            this.Dispose();
        }

        ConsoleApp.Current.Invoke(async () =>
        {
            while (this.IsExpired == false)
            {
                Evaluate();
                await Task.Yield();
            }
        });
    }

    private DateTime? last;
    private void Evaluate()
    {
        if (!IsPermanentForce && DateTime.Now >= EndTime)
        {
            this.Dispose();
            return;
        }


        float dt = last.HasValue ? (float)(DateTime.Now - last.Value).TotalSeconds : 0;
        float dSpeed = (Accelleration * dt);
        var end = tracker.Collider.Bounds.OffsetByAngleAndDistance(tracker.Angle, tracker.Speed).OffsetByAngleAndDistance(Angle, dSpeed);
        var newAngle = tracker.Collider.Bounds.CalculateAngleTo(end);
        var newSpeed = tracker.Collider.Bounds.CalculateDistanceTo(end);
        tracker.Angle = newAngle;
        tracker.Speed = newSpeed;
        last = DateTime.Now;
    }
}

