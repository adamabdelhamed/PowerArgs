using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames.Shooter 
{
    public class TimedMine : Explosive
    {
        private TimeSpan timeToDetinate;
        private TimeSpan startTime;
        public TimedMine(TimeSpan timeToDetinate, float x, float y, float angleIcrement, float range) : base(x,y, angleIcrement, range)
        {
            this.timeToDetinate = timeToDetinate;
        }

        public override void Initialize()
        {
            base.Initialize();
            this.startTime = Time.CurrentTime.Now;
        }

        public override void Evaluate()
        {
            base.Evaluate();
            if (Time.CurrentTime.Now - startTime >= timeToDetinate)
            {
                Explode();
            }
        }
    }
}
