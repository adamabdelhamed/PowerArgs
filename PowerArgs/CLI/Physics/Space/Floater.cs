using System;

namespace PowerArgs.Cli.Physics
{
    public class Floater : SpacialElementFunction
    {
        static Random rand = new Random();

        SpeedTracker tracker;
        public float MaxFloat { get; set; }

        public Floater(SpacialElement t, SpeedTracker tracker, float maxFloat = 1) : base(t)
        {
            this.MaxFloat = maxFloat;
            this.Governor.Rate = TimeSpan.FromSeconds(.03);
            this.tracker = tracker;
        }

        public override void Initialize()
        {
        }

        public override void Evaluate()
        {
            float dX = ((float)(rand.NextDouble())) * MaxFloat;
            float dY = ((float)(rand.NextDouble())) * MaxFloat;

            if (rand.NextDouble() > .5) dX = -dX;
            if (rand.NextDouble() > .5) dY = -dY;

            tracker.SpeedX += dX;
            tracker.SpeedY += dY;
        }
    }
}
