using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    internal static class Friendlies
    {
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

        private static int Round(this double number)
        {
            return (int)Math.Round(number);
        }
    }
}
