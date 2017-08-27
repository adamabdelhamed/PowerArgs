using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli.Physics
{
    public class RateGovernor
    {
        TimeSpan lastFire;
        public TimeSpan Rate { get; set; }

        public RateGovernor(float rateInSeconds = 0)
        {
            Rate = TimeSpan.FromSeconds(rateInSeconds);
        }

        public bool ShouldFire(TimeSpan currentTime)
        {
            if (currentTime - lastFire < Rate) return false;
            lastFire = currentTime;
            return true;
        }
    }

    public class WallClockRateGovernor
    {
        public float Rate { get; set; }
        public Stopwatch sw;

        public WallClockRateGovernor(float rateInSeconds)
        {
            Rate = rateInSeconds;
            sw = new Stopwatch();
            sw.Start();
        }

        public bool ShouldFire()
        {
            if (sw.Elapsed.TotalSeconds > Rate)
            {
                sw.Restart();
                return true;
            }

            return false;
        }
    }
}
