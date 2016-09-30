using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class Event
    {
        protected Dictionary<Action, Subscription> subscribers;

        public bool HasSubscriptions
        {
            get
            {
                return subscribers.Count > 0;
            }
        }

        public void SubscribeForLifetime(object onAdded, LifetimeManager lifetimeManager)
        {
            throw new NotImplementedException();
        }

        public void Fire()
        {
            foreach (var subscriber in subscribers.Keys.ToArray())
            {
                subscriber();
            }
        }

        public Event()
        {
            subscribers = new Dictionary<Action, Subscription>();
        }

        public Subscription SubscribeUnmanaged(Action handler)
        {
            var sub = new Subscription(() => { subscribers.Remove(handler); });
            subscribers.Add(handler, sub);
            return sub;
        }

        public void SubscribeForLifetime(Event removed, LifetimeManager lifetimeManager)
        {
            throw new NotImplementedException();
        }

        public void SubscribeForLifetime(Action handler, LifetimeManager lifetimeManager)
        {
            var sub = SubscribeUnmanaged(handler);
            lifetimeManager.Manage(sub);
        }

        public void Unsubscribe(Action handler)
        {
            subscribers[handler].Dispose();
        }
    }
    
    public class Event<T>
    {
        public bool HasSubscriptions
        {
            get
            {
                return subscribers.Count > 0;
            }
        }

        public void Fire(T item)
        {
            foreach (var subscriber in subscribers.Keys.ToArray())
            {
                subscriber(item);
            }
        }

        protected Dictionary<Action<T>, Subscription> subscribers;
        public Event()
        {
            subscribers = new Dictionary<Action<T>, Subscription>();
        }

        public Subscription SubscribeUnmanaged(Action<T> handler)
        {
            var sub = new Subscription(() => { subscribers.Remove(handler); });
            subscribers.Add(handler, sub);
            return sub;
        }

        public void SubscribeForLifetime(Action<T> handler, LifetimeManager lifetimeManager)
        {
            var sub = SubscribeUnmanaged(handler);
            lifetimeManager.Manage(sub);
        }

        public void Unsubscribe(Action<T> handler)
        {
            subscribers[handler].Dispose();
        }
    }
}
