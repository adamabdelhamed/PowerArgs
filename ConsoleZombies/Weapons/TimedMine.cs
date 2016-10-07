using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class TimedMine : Explosive
    {
        private TimeSpan timeToDetinate;
        private TimeSpan startTime;
        public TimedMine(TimeSpan timeToDetinate, Rectangle bounds, float angleIcrement, float range) : base(bounds, angleIcrement, range)
        {
            this.timeToDetinate = timeToDetinate;
        }

        public override void InitializeThing(Scene r)
        {
            base.InitializeThing(r);
            this.startTime = r.ElapsedTime;
        }

        public override void Behave(Scene r)
        {
            base.Behave(r);
            if(r.ElapsedTime - startTime >= timeToDetinate)
            {
                Explode();
            }
        }
    }
}
