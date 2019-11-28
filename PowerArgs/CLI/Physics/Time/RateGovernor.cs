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

        /// <summary>
        /// Creates a new rate governor
        /// </summary>
        /// <param name="rate">the minimum time between executions</param>
        public RateGovernor(TimeSpan rate, TimeSpan? lastFireTime = null)
        {
            Rate = rate;
            if(lastFireTime.HasValue)
            {
                lastFire = lastFireTime.Value;
            }
        }

        /// <summary>
        /// Resets the governor as if it fired just now
        /// </summary>
        /// <param name="currentTime">now</param>
        public void Reset(TimeSpan currentTime)
        {
            lastFire = currentTime;
        }

        /// <summary>
        /// Determines if enough time has passed per the governor's
        /// configured rate
        /// </summary>
        /// <param name="currentTime">the current time</param>
        /// <returns>True if enough time has passed, false otherwise</returns>
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
