using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// A lifetime aware event
    /// </summary>
    public class Event
    {

        private int tail;
        private int subCount;
        private (Action, ILifetimeManager)[] subscribers;

        /// <summary>
        /// returns true if there is at least one subscriber
        /// </summary>
        public bool HasSubscriptions => subCount > 0;
         
        /// <summary>
        /// Fires the event. All subscribers will be notified
        /// </summary>
        public void Fire()
        {
            for (var i = 0; i < tail; i++)
            {
                subscribers[i].Item1?.Invoke();
            }
        }

        /// <summary>
        /// Creates a new event
        /// </summary>
        public Event()
        {
            subscribers = new (Action, ILifetimeManager)[10];
        }

        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires 
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <returns>A subscription that can be disposed when you no loner want to be notified from this event</returns>
        public ILifetime SubscribeUnmanaged(Action handler)
        {
            EnsureRoomForMore();
            var sub = new Lifetime();
            var myI = tail++;
            subCount++;
            subscribers[myI] = (handler, sub);
            sub.OnDisposed(() =>
            {
                subscribers[myI] = default;
                subCount--;
            });
            return sub;
        }

        private void EnsureRoomForMore()
        {
            if(tail == subscribers.Length)
            {
                var tmp = subscribers;
                subscribers = new (Action, ILifetimeManager)[tmp.Length * 2];
                Array.Copy(tmp, subscribers, tmp.Length);
            }
        }

        public ILifetime SynchronizeUnmanaged(Action handler)
        {
            handler();
            return SynchronizeUnmanaged(handler);
        }


        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires. Notifications will stop
        /// when the lifetime associated with the given lifetime manager is disposed.
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <param name="lifetimeManager">the lifetime manager that determines when to stop being notified</param>
        public void SubscribeForLifetime(Action handler, ILifetimeManager lifetimeManager)
        {
            if (lifetimeManager.IsExpired == false)
            {
                EnsureRoomForMore();
                var myI = tail++;
                subCount++;
                subscribers[myI] = (handler, lifetimeManager);
                lifetimeManager.OnDisposed(() =>
                {
                    subscribers[myI] = default;
                    subCount--;
                });
            }
        }

        public void SynchronizeForLifetime(Action handler, ILifetimeManager lifetimeManager)
        {
            handler();
            SubscribeForLifetime(handler, lifetimeManager);
        }

        /// <summary>
        /// Subscribes to the event for one notification and then immediately unsubscribes so your callback will only be called at most once
        /// </summary>
        /// <param name="handler">The action to run when the event fires</param>
        public void SubscribeOnce(Action handler)
        {
            Action wrappedAction = null;
            var lt = new Lifetime();
            wrappedAction = () =>
            {
                try
                {
                    handler();
                }
                finally
                {
                    lt.Dispose();
                }
            };

            SubscribeForLifetime(wrappedAction, lt);
        }

        /// <summary>
        /// Creates a lifetime that will end the next time this
        /// event fires
        /// </summary>
        /// <returns>a lifetime that will end the next time this event fires</returns>
        public Lifetime CreateNextFireLifetime()
        {
            var lifetime = new Lifetime();
            this.SubscribeOnce(lifetime.Dispose);
            return lifetime;
        }
    }

    /// <summary>
    /// A lifetime aware event that can deliver a data payload to subscribers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Event<T>
    {

        private Dictionary<Action<T>, ILifetimeManager> subscribers;

        /// <summary>
        /// returns true if there is at least one subscriber
        /// </summary>
        public bool HasSubscriptions
        {
            get
            {
                return subscribers.Count > 0;
            }
        }

        /// <summary>
        /// Fires the event and delivers the given data payload to all subscribers
        /// </summary>
        /// <param name="item">the data payload</param>
        public void Fire(T item)
        {
            var subs = subscribers?.Keys?.ToArray();
            if (subs != null)
            {
                for (var i = 0; i < subs.Length; i++)
                {
                    subs[i]?.Invoke(item);
                }
            }
        }

        /// <summary>
        /// Creates the event
        /// </summary>
        public Event()
        {
            subscribers = new Dictionary<Action<T>, ILifetimeManager>();
        }


        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires 
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <returns>a subscription that can be disposed when you no longer want to be notified from this event</returns>
        public ILifetime SubscribeUnmanaged(Action<T> handler)
        {
            var sub = new Lifetime();
            sub.OnDisposed(() => subscribers.Remove(handler));
            subscribers.Add(handler, sub);
            return sub;
        }

        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires. Notifications will stop
        /// when the lifetime associated with the given lifetime manager is disposed.
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <param name="lifetimeManager">the lifetime manager that determines when to stop being notified by this event</param>
        public void SubscribeForLifetime(Action<T> handler, ILifetimeManager lifetimeManager)
        {
            if (lifetimeManager.IsExpired == false)
            {
                subscribers.Add(handler, lifetimeManager);
                lifetimeManager.OnDisposed(() => subscribers.Remove(handler));
            }
        }

        /// <summary>
        /// Subscribes to the event for one notification and then immediately unsubscribes so your callback will only be called at most once
        /// </summary>
        /// <param name="handler">The action to run when the event fires</param>
        public void SubscribeOnce(Action<T> handler)
        {
            Action<T> wrappedAction = null;
            wrappedAction = (t) =>
            {
                handler(t);
                subscribers.Remove(wrappedAction);
            };

            SubscribeUnmanaged(wrappedAction);
        }

        /// <summary>
        /// Creates a lifetime that will end the next time this
        /// event fires
        /// </summary>
        /// <returns>a lifetime that will end the next time this event fires</returns>
        public Lifetime CreateNextFireLifetime()
        {
            var lifetime = new Lifetime();
            this.SubscribeOnce(t => lifetime.Dispose());
            return lifetime;
        }
    }
}
