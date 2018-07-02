using System;

namespace PowerArgs.Cli.Physics
{
    /// <summary>
    /// A rate regulator
    /// </summary>
    public class RateGovernor
    {
        private TimeSpan lastFire;

        /// <summary>
        /// The regulation rate
        /// </summary>
        public TimeSpan Rate { get; set; }

        public RateGovernor(TimeSpan rate) { Rate = rate; }

        public bool ShouldFire(TimeSpan currentTime)
        {
            if (currentTime - lastFire < Rate)
            {
                return false;
            }
            else
            {
                lastFire = currentTime;
                return true;
            }
        }
    }
}
