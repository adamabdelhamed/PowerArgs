using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Reflection;

namespace PowerArgs.Cli
{
    public interface IObservableObject
    {
        bool SuppressEqualChanges { get; set; }
        IDisposable SubscribeUnmanaged(string propertyName, Action handler);
        void SubscribeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager);
        IDisposable SynchronizeUnmanaged(string propertyName, Action handler);
        void SynchronizeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager);
        object GetPrevious(string propertyName);

    }

    /// <summary>
    /// A class that makes it easy to define an object with observable properties
    /// </summary>
    public class ObservableObject : Lifetime, IObservableObject
    {
        /// <summary>
        /// Subscribe or synchronize using this key to receive notifications when any property changes
        /// </summary>
        public const string AnyProperty = "*";

        private Dictionary<string, List<PropertyChangedSubscription>> subscribers;
        private Dictionary<string, object> values;
        private Dictionary<string, object> previousValues;

        /// <summary>
        /// Set to true if you want to suppress notification events for properties that get set to their existing values.
        /// </summary>
        [FormIgnore]
        public bool SuppressEqualChanges { get; set; }

        /// <summary>
        /// DeepObservableRoot
        /// </summary>
        public IObservableObject DeepObservableRoot { get; private set; }

        /// <summary>
        /// Creates a new bag and optionally sets the notifier object.
        /// </summary>
        public ObservableObject(IObservableObject proxy = null)
        {
            SuppressEqualChanges = true;
            subscribers = new Dictionary<string, List<PropertyChangedSubscription>>();
            values = new Dictionary<string, object>();
            previousValues = new Dictionary<string, object>();
            DeepObservableRoot = proxy;
        }

        /// <summary>
        /// This should be called by a property getter to get the value
        /// </summary>
        /// <typeparam name="T">The type of property to get</typeparam>
        /// <param name="name">The name of the property to get</param>
        /// <returns>The property's current value</returns>
        public T Get<T>([CallerMemberName]string name = "")
        {
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
        /// Gets the previous value of the given property name
        /// </summary>
        /// <typeparam name="T">the type of property to get</typeparam>
        /// <param name="name">the name of the property</param>
        /// <returns>the previous value or default(T) if there was none</returns>
        public T GetPrevious<T>([CallerMemberName]string name = "")
        {
            object ret;
            if (previousValues.TryGetValue(name, out ret))
            {
                return (T)ret;
            }
            else
            {
                return default(T);
            }
        }

        object IObservableObject.GetPrevious(string name) => this.GetPrevious<object>(name);

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

            if (values.ContainsKey(name))
            {
                var previousValue = values[name];
                if (SuppressEqualChanges == false || isEqualChange == false)
                {
                    previousValues[name] = current;
                }
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

        /// <summary>
        /// Subscribes to be notified when the given property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property to subscribe to or ObservableObject.AnyProperty if you want to be notified of any property change.</param>
        /// <param name="handler">The action to call for notifications</param>
        /// <returns>A subscription that will receive notifications until it is disposed</returns>
        public IDisposable SubscribeUnmanaged(string propertyName, Action handler)
        {
            if (propertyName.Contains(".") == false)
            {
                var sub = new PropertyChangedSubscription(propertyName, handler, CleanupSubscription);

                List<PropertyChangedSubscription> subsForProperty;
                if (subscribers.TryGetValue(propertyName, out subsForProperty) == false)
                {
                    subsForProperty = new List<PropertyChangedSubscription>();
                    subscribers.Add(propertyName, subsForProperty);
                }

                subsForProperty.Add(sub);

                return sub;
            }
            else
            {
                return DeepSubscribeInternal(propertyName, handler);
            }
        }

        private IDisposable DeepSubscribeInternal(string path, Action handler, Lifetime rootLifetime = null)
        {
            rootLifetime = rootLifetime ?? new Lifetime();
            var observableRoot = DeepObservableRoot ?? this;
            IObservableObject currentObservable = observableRoot;
            var pathExpression = ObjectPathExpression.Parse(path);
            List<IObjectPathElement> currentPath = new List<IObjectPathElement>();

            var anyChangeLifetime = new Lifetime();
            for (int i = 0; i < pathExpression.Elements.Count; i++)
            {
                var propertyElement = pathExpression.Elements[i] as PropertyPathElement;
                if (propertyElement == null)
                {
                    throw new NotSupportedException("Indexers are not supported for deep observability");
                }

                currentPath.Add(propertyElement);

                ObjectPathExpression.TraceNode eval;

                if (i == pathExpression.Elements.Count - 1 && propertyElement.PropertyName == ObservableObject.AnyProperty)
                {
                    eval = new ObjectPathExpression.TraceNode() { Value = null };
                }
                else
                {
                    eval = new ObjectPathExpression(currentPath).EvaluateAndTraceInfo(observableRoot).Last();
                    if (i != pathExpression.Elements.Count - 1 && (eval.MemberInfo as PropertyInfo).PropertyType.GetInterfaces().Contains(typeof(IObservableObject)) == false)
                    {
                        throw new NotSupportedException($"element {eval.MemberInfo.Name} is not observable");
                    }
                }

                currentObservable.SubscribeForLifetime(propertyElement.PropertyName, () =>
                {
                    handler();
                    if (anyChangeLifetime.IsExpired == false)
                    {
                        anyChangeLifetime.Dispose();
                    }

                    if (rootLifetime.IsExpired == false)
                    {
                        DeepSubscribeInternal(path, handler, rootLifetime);
                    }
                }, EarliestOf(anyChangeLifetime, rootLifetime));

                currentObservable = eval.Value as IObservableObject;

                if(currentObservable == null)
                {
                    break;
                }
            }

            return rootLifetime;
        }

        /// <summary>
        /// Subscribes to be notified when the given property changes.  The subscription expires when
        /// the given lifetime manager's lifetime ends.
        /// </summary>
        /// <param name="propertyName">The name of the property to subscribe to or ObservableObject.AnyProperty if you want to be notified of any property change.</param>
        /// <param name="handler">The action to call for notifications</param>
        /// <param name="lifetimeManager">the lifetime manager that determines when the subscription ends</param>
        public void SubscribeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager)
        {
            var sub = SubscribeUnmanaged(propertyName, handler);
            lifetimeManager.OnDisposed(sub);
        }

        /// <summary>
        /// Subscribes to be notified when the given property changes and also fires an initial notification immediately.
        /// </summary>
        /// <param name="propertyName">The name of the property to subscribe to or ObservableObject.AnyProperty if you want to be notified of any property change.</param>
        /// <param name="handler">The action to call for notifications</param>
        /// <returns>A subscription that will receive notifications until it is disposed</returns>
        public IDisposable SynchronizeUnmanaged(string propertyName, Action handler)
        {
            handler();
            return SubscribeUnmanaged(propertyName, handler);
        }

        /// <summary>
        /// Subscribes to be notified when the given property changes and also fires an initial notification.  The subscription expires when
        /// the given lifetime manager's lifetime ends.
        /// </summary>
        /// <param name="propertyName">The name of the property to subscribe to or ObservableObject.AnyProperty if you want to be notified of any property change.</param>
        /// <param name="handler">The action to call for notifications</param>
        /// <param name="lifetimeManager">the lifetime manager that determines when the subscription ends</param>

        public void SynchronizeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager)
        {
            var sub = SynchronizeUnmanaged(propertyName, handler);
            lifetimeManager.OnDisposed(sub);
        }

        public Lifetime GetPropertyValueLifetime(string propertyName)
        {
            Lifetime ret = new Lifetime();
            IDisposable sub = null;
            sub = SubscribeUnmanaged(propertyName, () =>
            {
                sub.Dispose();
                ret.Dispose();
            });

            return ret;
        }
       
        /// <summary>
        /// Fires the PropertyChanged event with the given property name.
        /// </summary>
        /// <param name="propertyName">the name of the property that changed</param>
        public void FirePropertyChanged(string propertyName)
        {
            List<PropertyChangedSubscription> filteredSubs;
            if(subscribers.TryGetValue(propertyName, out filteredSubs))
            {
                foreach (var sub in filteredSubs.ToArray())
                {
                    sub.ChangeListener();
                }
            }

            if(subscribers.TryGetValue(AnyProperty, out filteredSubs))
            {
                foreach (var sub in filteredSubs.ToArray())
                {
                    sub.ChangeListener();
                }
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
        public static bool EqualsSafe(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null ^ b == null) return false;
            if (object.ReferenceEquals(a, b)) return true;

            return a.Equals(b);
        }

        private void CleanupSubscription(PropertyChangedSubscription sub)
        {
            List<PropertyChangedSubscription> subsForProperty;
            if (subscribers.TryGetValue(sub.PropertyName, out subsForProperty) == false)
            {
                throw new KeyNotFoundException("The given subscription was not found");
            }

            subsForProperty.Remove(sub);
            if (subsForProperty.Count == 0)
            {
                subscribers.Remove(sub.PropertyName);
            }
        }

    }
}
