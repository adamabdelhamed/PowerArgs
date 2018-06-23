using System;

namespace PowerArgs.Cli.Physics
{
    /// <summary>
    /// A rate regulator
    /// </summary>
    public class RateGovernor
    {
        TimeSpan lastFire;

        /// <summary>
        /// The regulation rate
        /// </summary>
        public TimeSpan Rate { get; set; }

        public RateGovernor(TimeSpan rate) { Rate = rate; }

        public bool ShouldFire(TimeSpan currentTime)
        {
            if (currentTime - lastFire < Rate) return false;
            lastFire = currentTime;
            return true;
        }
    }
}
