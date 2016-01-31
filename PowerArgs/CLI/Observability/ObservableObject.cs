using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A helper that makes it easy to define an object with observable properties
    /// </summary>
    public class ObservableObject : Lifetime
    {
        private Dictionary<string, List<PropertyChangedSubscription>> subscribers;

        /// <summary>
        /// A handler that a child class can use to be notified before a value changes.
        /// </summary>
        protected Func<object, string, bool> ProtectedPropertyChangingHandler;

        /// <summary>
        /// An event that fires when a property is accessed
        /// </summary>
        public event PropertyChangedEventHandler PropertyAccessed;

        /// <summary>
        /// The object to be used as the sender for notification events (defaults to this).
        /// </summary>
        public object NotifierObject { get; private set; }

        /// <summary>
        /// Set to true if you want to suppress notification events for properties that get set to their existing values.
        /// </summary>
        public bool SuppressEqualChanges { get; set; }

        private Dictionary<string, object> values = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new bag and optionally sets the notifier object.
        /// </summary>
        /// <param name="sender">The object to be used as the sender for notification events (defaults to this)</param>
        public ObservableObject(object sender = null)
        {
            NotifierObject = sender ?? this;
            SuppressEqualChanges = true;
            subscribers = new Dictionary<string, List<PropertyChangedSubscription>>();
        }

        /// <summary>
        /// This should be called by a property getter to get the value
        /// </summary>
        /// <typeparam name="T">The type of property to get</typeparam>
        /// <param name="name">The name of the property to get</param>
        /// <returns>The property's current value</returns>
        public T Get<T>([CallerMemberName]string name = "")
        {
            if (PropertyAccessed != null)
            {
                PropertyAccessed(NotifierObject, new PropertyChangedEventArgs(name));
            }

            object ret;
            if(values.TryGetValue(name, out ret))
            {
                return (T)ret;
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// This should be called by a property getter to set the value.
        /// </summary>
        /// <typeparam name="T">The type of property to set</typeparam>
        /// <param name="value">The value to set</param>
        /// <param name="name">The name of the property to set</param>
        public void Set<T>(T value,[CallerMemberName] string name = "")
        {
            var current = Get<T>(name);
            var isEqualChange = EqualsSafe(current, value);

            if (SuppressEqualChanges == false || isEqualChange == false)
            {
                if (ProtectedPropertyChangingHandler != null)
                {
                    var shouldCancel = ProtectedPropertyChangingHandler(NotifierObject, name);
                    if (shouldCancel)
                    {
                        return;
                    }
                }
            }

            if (values.ContainsKey(name))
            {
                values[name] = value;
            }
            else
            {
                values.Add(name, value);
            }

            if (SuppressEqualChanges == false || isEqualChange == false)
            {
                FirePropertyChanged(name);
            }
        }

        public void SubscribeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager)
        {
            var sub = SubscribeUnmanaged(propertyName, handler);
            lifetimeManager.Manage(sub);
        }

        public void SynchronizeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager)
        {
            handler();
            SubscribeForLifetime(propertyName, handler, lifetimeManager);
        }

        public void Subscribe(string propertyName, Action handler)
        {
            SubscribeForLifetime(propertyName, handler, LifetimeManager.AmbientLifetimeManager);
        }

        public void Synchronize(string propertyName, Action handler)
        {
            handler();
            Subscribe(propertyName, handler);
        }

        

        public PropertyChangedSubscription SubscribeUnmanaged(string propertyName, Action handler)
        {
            var sub = new PropertyChangedSubscription(propertyName, handler, Unsubscribe);

            List<PropertyChangedSubscription> subsForProperty;
            if(subscribers.TryGetValue(propertyName, out subsForProperty) == false)
            {
                subsForProperty = new List<PropertyChangedSubscription>();
                subscribers.Add(propertyName, subsForProperty);
            }
            subsForProperty.Add(sub);

            return sub;
        }

        private void Unsubscribe(PropertyChangedSubscription sub)
        {
            List<PropertyChangedSubscription> subsForProperty;
            if (subscribers.TryGetValue(sub.PropertyName, out subsForProperty) == false)
            {
                throw new KeyNotFoundException("The given subscription was not found");
            }

            subsForProperty.Remove(sub);
            if(subsForProperty.Count == 0)
            {
                subscribers.Remove(sub.PropertyName);
            }
        }

        public PropertyChangedSubscription SynchronizeUnmanaged(string propertyName, Action handler)
        {
            handler();
            return SubscribeUnmanaged(propertyName, handler);
        }

        /// <summary>
        /// Fires the PropertyChanged event with the given property name.
        /// </summary>
        /// <param name="propertyName">the name of the property that changed</param>
        protected void FirePropertyChanged(string propertyName)
        {
            List<PropertyChangedSubscription> filteredSubs;
            if(subscribers.TryGetValue(propertyName, out filteredSubs) == false && subscribers.TryGetValue("*", out filteredSubs) == false)
            {
                return;
            }

            foreach(var sub in filteredSubs)
            {
                sub.ChangeListener();
            }
        }

        /// <summary>
        /// A generic equals implementation that allows nulls to be passed for either parameter.  Objects should not call this from
        /// within their own equals method since that will cause a stack overflow.  The Equals() functions do not get called if the two
        /// inputs reference the same object.
        /// </summary>
        /// <param name="a">The first object to test</param>
        /// <param name="b">The second object to test</param>
        /// <returns>True if the values are equal, false otherwise.</returns>
        private static bool EqualsSafe(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null ^ b == null) return false;
            if (object.ReferenceEquals(a, b)) return true;

            return a.Equals(b);
        }
    }
}
