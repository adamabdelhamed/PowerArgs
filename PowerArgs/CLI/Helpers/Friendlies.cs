using System;

namespace PowerArgs.Cli
{
    /// <summary>
    /// Helper functions for formatting values in more user friendly terms
    /// </summary>
    public static class Friendlies
    {
        /// <summary>
        /// Returns a string that represents the difference in time between this time and DateTime.Now
        /// 
        /// a simple subtraction operator is used. 
        /// </summary>
        /// <param name="time">a time in the past</param>
        /// <returns> A friendly relative time description (e.g. '1 day ago')</returns>
        public static string ToFriendlyPastTimeStamp(this DateTime time)
        {
            var now = DateTime.Now;

            var delta = now - time;

            if(delta < TimeSpan.FromSeconds(30))
            {
                return "just now";
            }
            else if(delta < TimeSpan.FromSeconds(120))
            {
                return $"{delta.TotalSeconds.Round()} seconds ago";
            }
            else if (delta < TimeSpan.FromMinutes(120))
            {
                return $"{delta.TotalMinutes.Round()} minutes ago";
            }
            else if (delta < TimeSpan.FromHours(48))
            {
                return $"{delta.TotalHours.Round()} hours ago";
            }
            else
            {
                return $"{delta.TotalDays.Round()} days ago";
            }
        }

        private static int Round(this double number) => (int)Math.Round(number);
    }
}
