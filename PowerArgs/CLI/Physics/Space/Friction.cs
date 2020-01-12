namespace PowerArgs.Cli.Physics
{
    public class Friction : SpacialElementFunction
    {
        public float Decay { get; set; } = .9f;

        private Velocity tracker;
        public Friction(Velocity tracker) : base(tracker.Element)
        {
            this.tracker = tracker;
            tracker.Lifetime.OnDisposed(this.Lifetime.Dispose);
        }

        public override void Evaluate()
        {
            tracker.Speed *= Decay;
        }
    }
}
