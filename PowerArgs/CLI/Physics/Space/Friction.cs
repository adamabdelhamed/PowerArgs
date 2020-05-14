namespace PowerArgs.Cli.Physics
{
    public class Friction : SpacialElementFunction
    {
        public const int DefaultFrictionEvalFrequency = 50;
        public float Decay { get; set; } = .9f;

        private Velocity tracker;
        public Friction(Velocity tracker, float evalFrequency = DefaultFrictionEvalFrequency) : base(tracker.Element)
        {
            this.tracker = tracker;
            tracker.Lifetime.OnDisposed(this.Lifetime.Dispose);

            this.Added.SubscribeOnce(async () =>
            {
                while (this.Lifetime.IsExpired == false)
                {
                    Evaluate();
                    await Time.CurrentTime.DelayAsync(evalFrequency);
                }
            });
        }

        private void Evaluate()
        {
            tracker.Speed *= Decay;
            if (tracker.Speed < .1f) tracker.Speed = 0;
        }
    }
}
