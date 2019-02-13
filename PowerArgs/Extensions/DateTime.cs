using System;

namespace PowerArgs
{
    /// <summary>
    /// Extensions for date times and time spans
    /// </summary>
    public static class DateExtensions
    {
        /// <summary>
        /// Rounds the given date time to the given time span
        /// </summary>
        /// <param name="date">the date to round</param>
        /// <param name="span">The interval to round to</param>
        /// <returns>the rounded date time</returns>
        public static DateTime Round(this DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks);
        }
        /// <summary>
        /// Gets the floor value of the given date time using a given time span to decide the granularity
        /// </summary>
        /// <param name="date">the date to floor</param>
        /// <param name="span">the granularity of the floor function</param>
        /// <returns>the floored value</returns>
        public static DateTime Floor(this DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks / span.Ticks);
            return new DateTime(ticks * span.Ticks);
        }

        /// <summary>
        /// Gets the ceiling value of the given date time using a given time span to decide the granularity
        /// </summary>
        /// <param name="date">the date to ceiling</param>
        /// <param name="span">the granularity of the ceiling function</param>
        /// <returns>the ceiling value</returns>
        public static DateTime Ceil(this DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks + span.Ticks - 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks);
        }

        /// <summary>
        /// Rounds the given time span
        /// </summary>
        /// <param name="value">the timespan to round</param>
        /// <param name="span">The interval to round to</param>
        /// <returns>the rounded time span</returns>
        public static TimeSpan Round(this TimeSpan value, TimeSpan span)
        {
            long ticks = (value.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
            return new TimeSpan(ticks * span.Ticks);
        }

        /// <summary>
        /// Gets the floor value of the given time span using a given time span to decide the granularity
        /// </summary>
        /// <param name="value">the time span to floor</param>
        /// <param name="span">the granularity of the floor function</param>
        /// <returns>the floored value</returns>
        public static TimeSpan Floor(this TimeSpan value, TimeSpan span)
        {
            long ticks = (value.Ticks / span.Ticks);
            return new TimeSpan(ticks * span.Ticks);
        }

        /// <summary>
        /// Gets the ceiling value of the given time span using a given time span to decide the granularity
        /// </summary>
        /// <param name="value">the time span to ceiling</param>
        /// <param name="span">the granularity of the ceiling function</param>
        /// <returns>the ceiling value</returns>
        public static TimeSpan Ceil(this TimeSpan value, TimeSpan span)
        {
            long ticks = (value.Ticks + span.Ticks - 1) / span.Ticks;
            return new TimeSpan(ticks * span.Ticks);
        }
    }
}
