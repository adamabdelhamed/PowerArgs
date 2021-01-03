using System;
using System.Threading.Tasks;

namespace PowerArgs
{  
    /// <summary>
   /// A class that can be used to ensure an action only executes after a burst of triggers ends.
   /// </summary>
    public class ActionDebouncer
    {
        private TimeSpan burstTimeWindow;
        private Action callback;
        private object latestRequest;
        private TimeSpan? guarantee;
        private DateTime? oldestPendingRequest;
        private object lck;

        /// <summary>
        /// Creates a new action debouncer.
        /// </summary>
        /// <param name="burstTimeWindow">the time to wait before executing because there might be other triggers that come soon.</param>
        /// <param name="callback">the action to execute once debouncing has been applied</param>
        /// <param name="guarantee">and optional guarantee that will ensure the callback can execute periodically in the event that the trigger is called continuously</param>
        public ActionDebouncer(TimeSpan burstTimeWindow, Action callback, TimeSpan? guarantee = null)
        {
            this.guarantee = guarantee;
            this.burstTimeWindow = burstTimeWindow;
            this.callback = callback;
            lck = new object();
        }

        private bool IsGuaranteeDue => guarantee.HasValue && oldestPendingRequest.HasValue && DateTime.UtcNow - oldestPendingRequest.Value >= guarantee.Value;

        private void MakeCallback()
        {
            latestRequest = null;
            oldestPendingRequest = null;
            callback();
        }

        /// <summary>
        /// Triggers the debouncer to execute the wrapped action.
        /// </summary>
        public async void Trigger()
        {
            var myRequest = new object();
            var makeCallback = false;

            lock (lck)
            {
                latestRequest = myRequest;
                makeCallback = burstTimeWindow == TimeSpan.Zero || IsGuaranteeDue;
            }

            if (makeCallback)
            {
                MakeCallback();
                return;
            }

            // keep track of the oldest pending request so that we can enforce our guarantee
            if (oldestPendingRequest.HasValue == false)
            {
                oldestPendingRequest = DateTime.UtcNow;
            }

            await Task.Delay(burstTimeWindow);

            lock (lck)
            {
                makeCallback = myRequest == latestRequest || IsGuaranteeDue;
            }

            if (makeCallback)
            {
                MakeCallback();
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
