using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A lifetime aware event
    /// </summary>
    public class Event
    {

        private Dictionary<Action, Subscription> subscribers;

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
        /// Fires the event. All subscribers will be notified
        /// </summary>
        public void Fire()
        {
            foreach (var subscriber in subscribers.Keys.ToArray())
            {
                subscriber();
            }
        }

        /// <summary>
        /// Creates a new event
        /// </summary>
        public Event()
        {
            subscribers = new Dictionary<Action, Subscription>();
        }

        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires 
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <returns>A subscription that can be disposed when you no loner want to be notified from this event</returns>
        public Subscription SubscribeUnmanaged(Action handler)
        {
            var sub = new Subscription(() => { subscribers.Remove(handler); });
            subscribers.Add(handler, sub);
            return sub;
        }

        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires. Notifications will stop
        /// when the lifetime associated with the given lifetime manager is disposed.
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <param name="lifetimeManager">the lifetime manager that determines when to stop being notified</param>
        public void SubscribeForLifetime(Action handler, LifetimeManager lifetimeManager)
        {
            var sub = SubscribeUnmanaged(handler);
            lifetimeManager.Manage(sub);
        }

        /// <summary>
        /// Subscribes to the event for one notification and then immediately unsubscribes so your callback will only be called at most once
        /// </summary>
        /// <param name="handler">The action to run when the event fires</param>
        public void SubscribeOnce(Action handler)
        {
            Action wrappedAction = null;
            wrappedAction = () =>
            {
                handler();
                subscribers.Remove(wrappedAction);
            };

            SubscribeUnmanaged(wrappedAction);
        }
    }
    
    /// <summary>
    /// A lifetime aware event that can deliver a data payload to subscribers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Event<T>
    {

        private Dictionary<Action<T>, Subscription> subscribers;

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
            foreach (var subscriber in subscribers.Keys.ToArray())
            {
                subscriber(item);
            }
        }

        /// <summary>
        /// Creates the event
        /// </summary>
        public Event()
        {
            subscribers = new Dictionary<Action<T>, Subscription>();
        }


        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires 
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <returns>a subscription that can be disposed when you no longer want to be notified from this event</returns>
        public Subscription SubscribeUnmanaged(Action<T> handler)
        {
            var sub = new Subscription(() => { subscribers.Remove(handler); });
            subscribers.Add(handler, sub);
            return sub;
        }

        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires. Notifications will stop
        /// when the lifetime associated with the given lifetime manager is disposed.
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <param name="lifetimeManager">the lifetime manager that determines when to stop being notified by this event</param>
        public void SubscribeForLifetime(Action<T> handler, LifetimeManager lifetimeManager)
        {
            var sub = SubscribeUnmanaged(handler);
            lifetimeManager.Manage(sub);
        }

        /// <summary>
        /// Unsubscribes manually. This is obsolete.
        /// </summary>
        /// <param name="handler">The handler that was used to subscribe previously</param>
        [Obsolete("You should use one of the lifetime aware subscribe methods and then unsubscribe using lifetimes")]
        public void Unsubscribe(Action<T> handler)
        {
            subscribers[handler].Dispose();
        }
    }
}
