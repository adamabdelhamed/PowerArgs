using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    internal class TimelineEvent
    {
        public string Component { get; private set; }
        public string Message { get; private set; }
        public DateTime Timestamp { get; private set; }
        public TimeSpan DeltaFromPreviousEvent { get; internal set; }

        public TimelineEvent(string message, string component)
        {
            this.Message = message;
            this.Component = component;
            this.Timestamp = DateTime.UtcNow;
        }
    }

    internal class AsyncTimeline
    {
        public static AsyncTimeline Current { get; set; }

        public List<TimelineEvent> Events { get; private set; }

        public AsyncTimeline()
        {
            Events = new List<TimelineEvent>();
        }

        public void AddEvent(string message, string component)
        {
            lock (Events)
            {
                var lastEvent = Events.Count == 0 ? null : Events.Last();
                var ev = new TimelineEvent(message, component);
                ev.DeltaFromPreviousEvent = lastEvent == null ? TimeSpan.Zero : ev.Timestamp - lastEvent.Timestamp;
                Events.Add(ev);
            }
        }

        public override string ToString()
        {
            var ret = "";
            foreach (var ev in Events)
            {
                ret += $"{ev.DeltaFromPreviousEvent.TotalMilliseconds} ms\t{ev.Component}\t{ev.Message}\n";
            }
            return ret;
        }
    }
}
