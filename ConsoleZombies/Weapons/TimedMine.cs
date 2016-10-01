using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs.Cli.Physics;

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

        public override void InitializeThing(Realm r)
        {
            base.InitializeThing(r);
            this.startTime = r.ElapsedTime;
        }

        public override void Behave(Realm r)
        {
            base.Behave(r);
            if(r.ElapsedTime - startTime >= timeToDetinate)
            {
                Explode();
            }
        }
    }
}
