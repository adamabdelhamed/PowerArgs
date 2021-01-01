using System;
using System.Threading;

namespace PowerArgs
{
    /// <summary>
    /// A class that can be used to ensure an action only fires after a burst of triggers ends.
    /// </summary>
    public class ActionDebouncer
    {
        /// <summary>
        /// Gets or sets the burst time window duration used to debounce multiple triggers
        /// </summary>
        public TimeSpan BurstTimeWindow { get; set; }

        private Action callback;

        private Timer endOfBurstTimerDetectionTimer;

        /// <summary>
        /// If you set this, then the debouncer will fire the callback in the event of an unexpectedly long burst occurs. Set the
        /// value to a time that would be too long to wait for the action to fire.
        /// </summary>
        public TimeSpan? Guarantee { get; set; }

        private DateTime? oldestPendingRequest;

        private object lck = new object();

        /// <summary>
        /// Creates the debouncer given a bust time window and an action callback.
        /// </summary>
        /// <param name="burstTimeWindow">The time span that determines the time window.  When a trigger fires, the debouncer will wait this amount of time before executing the callback.  If a trigger fires before the time elapses, the timer is reset.</param>
        /// <param name="callback">The action to execute when a trigger fires and the burst time window elapses.</param>
        public ActionDebouncer(TimeSpan burstTimeWindow, Action callback)
        {
            this.BurstTimeWindow = burstTimeWindow;
            this.callback = callback;
            this.endOfBurstTimerDetectionTimer = new Timer((o) => 
            {
                callback();

                if (Guarantee.HasValue)
                {
                    lock (lck)
                    {
                        oldestPendingRequest = null;
                    }
                }
            }, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Triggers a burst.  If this is the first trigger in a burst of events then a countdown of duration BurstTimeWindow begins.  If this trigger occurs
        /// while the countdown is in the process of counting to zero then the countdown is reset.  If the countdown ever hits zero then the callback fires
        /// and the burst is complete.
        /// </summary>
        public void Trigger()
        {
            if (Guarantee.HasValue)
            {
                lock (lck)
                {
                    if (oldestPendingRequest.HasValue == false)
                    {
                        oldestPendingRequest = DateTime.UtcNow;
                    }

                    if(DateTime.UtcNow - oldestPendingRequest >= Guarantee)
                    {
                        endOfBurstTimerDetectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        callback();
                    }
                }
            }

            if (BurstTimeWindow == TimeSpan.Zero)
            {
                callback();
            }
            else
            {
                endOfBurstTimerDetectionTimer.Change((int)BurstTimeWindow.TotalMilliseconds, Timeout.Infinite);
            }
        }
    }

    public class ActionThrottler
    {
        public TimeSpan BurstTimeWindow { get; set; }
        private Action callback;
        private DateTime lastFireTime;

        public ActionThrottler(TimeSpan burstTimeWindow, Action callback)
        {
            this.BurstTimeWindow = burstTimeWindow;
            lastFireTime = DateTime.MinValue;
            this.callback = callback;
        }

        public void Trigger()
        {
            var now = DateTime.UtcNow;
            if(now - lastFireTime >= BurstTimeWindow)
            {
                callback();
                lastFireTime = now;
            }
        }
    }

    public class ActionThrottler<T>
    {
        public TimeSpan BurstTimeWindow { get; set; }
        private Action<T> callback;
        private DateTime lastFireTime;

        public ActionThrottler(TimeSpan burstTimeWindow, Action<T> callback)
        {
            this.BurstTimeWindow = burstTimeWindow;
            lastFireTime = DateTime.MinValue;
            this.callback = callback;
        }

        public void Trigger(T input)
        {
            var now = DateTime.UtcNow;
            if (now - lastFireTime >= BurstTimeWindow)
            {
                callback(input);
                lastFireTime = now;
            }
        }
    }
}
