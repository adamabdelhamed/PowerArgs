using System;

namespace PowerArgs
{
    public static class DateExtensions
    {
        public static DateTime Round(this DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks);
        }
        public static DateTime Floor(this DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks / span.Ticks);
            return new DateTime(ticks * span.Ticks);
        }
        public static DateTime Ceil(this DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks + span.Ticks - 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks);
        }

        public static TimeSpan Round(this TimeSpan value, TimeSpan span)
        {
            long ticks = (value.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
            return new TimeSpan(ticks * span.Ticks);
        }
        public static TimeSpan Floor(this TimeSpan value, TimeSpan span)
        {
            long ticks = (value.Ticks / span.Ticks);
            return new TimeSpan(ticks * span.Ticks);
        }
        public static TimeSpan Ceil(this TimeSpan value, TimeSpan span)
        {
            long ticks = (value.Ticks + span.Ticks - 1) / span.Ticks;
            return new TimeSpan(ticks * span.Ticks);
        }
    }
}
