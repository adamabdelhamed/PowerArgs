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

        private int paramsTail;
        private int paramsSubCount;
        private (Action<object>, object, ILifetimeManager)[] subscribersWithParams;

        /// <summary>
        /// returns true if there is at least one subscriber
        /// </summary>
        public bool HasSubscriptions => subCount > 0 || paramsSubCount > 0;
         
        public Event()
        {

        }

        /// <summary>
        /// Fires the event. All subscribers will be notified
        /// </summary>
        public void Fire()
        {
            for (var i = 0; i < tail; i++)
            {
                subscribers[i].Item1?.Invoke();
            }

            for (var i = 0; i < paramsTail; i++)
            {
                subscribersWithParams[i].Item1?.Invoke(subscribersWithParams[i].Item2);
            }
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
            sub.OnDisposed(DisposeOf, myI);
            return sub;
        }

        public ILifetime SubscribeUnmanaged(Action<object> handler, object param)
        {
            EnsureRoomForMoreWithParams();
            var sub = new Lifetime();
            var myI = tail++;
            paramsSubCount++;
            subscribersWithParams[myI] = (handler, param, sub);
            sub.OnDisposed(DisposeOf, myI);
            return sub;
        }

        private void DisposeOf(object index)
        {
            subscribers[(int)index] = default;
            subCount--;
        }

        private void EnsureRoomForMore()
        {
            subscribers = subscribers ?? new (Action, ILifetimeManager)[10];
            if (tail == subscribers.Length)
            {
                var tmp = subscribers;
                subscribers = new (Action, ILifetimeManager)[tmp.Length * 2];
                Array.Copy(tmp, subscribers, tmp.Length);
            }
        }

        private void EnsureRoomForMoreWithParams()
        {
            subscribersWithParams = subscribersWithParams ?? new (Action<object>, object, ILifetimeManager)[10];
            if (paramsTail == subscribersWithParams.Length)
            {
                var tmp = subscribersWithParams;
                subscribersWithParams = new (Action<object>, object, ILifetimeManager)[tmp.Length * 2];
                Array.Copy(tmp, subscribersWithParams, tmp.Length);
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
            var lt = SubscribeUnmanaged(handler);
            lifetimeManager.OnDisposed(lt);
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

        public void SubscribeOnce(Action<object> handler, object param)
        {
            Action wrappedAction = null;
            var lt = new Lifetime();
            wrappedAction = () =>
            {
                try
                {
                    handler(param);
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

        public Task CreateNextFireTask()
        {
            var tcs = new TaskCompletionSource<bool>();
            this.SubscribeOnce(SetResultTrue, tcs);
            return tcs.Task;
        }

        private void SetResultTrue(object obj)
        {
            (obj as TaskCompletionSource<bool>).SetResult(true);
        }
    }

    public class Event<T>
    {

        private int tail;
        private int subCount;
        private (Action<T>, ILifetimeManager)[] subscribers;

        private int paramsTail;
        private int paramsSubCount;
        private (Action<T,object>, object, ILifetimeManager)[] subscribersWithParams;

        /// <summary>
        /// returns true if there is at least one subscriber
        /// </summary>
        public bool HasSubscriptions => subCount > 0 || paramsSubCount > 0;

        /// <summary>
        /// Fires the event. All subscribers will be notified
        /// </summary>
        public void Fire(T args)
        {
            for (var i = 0; i < tail; i++)
            {
                subscribers[i].Item1?.Invoke(args);
            }

            for (var i = 0; i < paramsTail; i++)
            {
                subscribersWithParams[i].Item1?.Invoke(args, subscribersWithParams[i].Item2);
            }
        }


        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires 
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <returns>A subscription that can be disposed when you no loner want to be notified from this event</returns>
        public ILifetime SubscribeUnmanaged(Action<T> handler)
        {
            EnsureRoomForMore();
            var sub = new Lifetime();
            var myI = tail++;
            subCount++;
            subscribers[myI] = (handler, sub);
            sub.OnDisposed(DisposeOf, myI);
            return sub;
        }

        public ILifetime SubscribeUnmanaged(Action<T,object> handler, object param)
        {
            EnsureRoomForMoreWithParams();
            var sub = new Lifetime();
            var myI = tail++;
            paramsSubCount++;
            subscribersWithParams[myI] = (handler, param, sub);
            sub.OnDisposed(DisposeOf, myI);
            return sub;
        }

        private void DisposeOf(object index)
        {
            subscribers[(int)index] = default;
            subCount--;
        }

        private void EnsureRoomForMore()
        {
            subscribers = subscribers ?? new (Action<T>, ILifetimeManager)[10];
            if (tail == subscribers.Length)
            {
                var tmp = subscribers;
                subscribers = new (Action<T>, ILifetimeManager)[tmp.Length * 2];
                Array.Copy(tmp, subscribers, tmp.Length);
            }
        }

        private void EnsureRoomForMoreWithParams()
        {
            subscribersWithParams = subscribersWithParams ?? new (Action<T,object>, object, ILifetimeManager)[10];
            if (paramsTail == subscribersWithParams.Length)
            {
                var tmp = subscribersWithParams;
                subscribersWithParams = new (Action<T,object>, object, ILifetimeManager)[tmp.Length * 2];
                Array.Copy(tmp, subscribersWithParams, tmp.Length);
            }
        }
 

        /// <summary>
        /// Subscribes to this event such that the given handler will be called when the event fires. Notifications will stop
        /// when the lifetime associated with the given lifetime manager is disposed.
        /// </summary>
        /// <param name="handler">the action to run when the event fires</param>
        /// <param name="lifetimeManager">the lifetime manager that determines when to stop being notified</param>
        public void SubscribeForLifetime(Action<T> handler, ILifetimeManager lifetimeManager)
        {
            var lt = SubscribeUnmanaged(handler);
            lifetimeManager.OnDisposed(lt);
        }

        /// <summary>
        /// Subscribes to the event for one notification and then immediately unsubscribes so your callback will only be called at most once
        /// </summary>
        /// <param name="handler">The action to run when the event fires</param>
        public void SubscribeOnce(Action<T> handler)
        {
            Action<T> wrappedAction = null;
            var lt = new Lifetime();
            wrappedAction = (args) =>
            {
                try
                {
                    handler(args);
                }
                finally
                {
                    lt.Dispose();
                }
            };

            SubscribeForLifetime(wrappedAction, lt);
        }

        public void SubscribeOnce(Action<T, object> handler, object param)
        {
            Action<T> wrappedAction = null;
            var lt = new Lifetime();
            wrappedAction = (args) =>
            {
                try
                {
                    handler(args, param);
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
            this.SubscribeOnce(args => lifetime.Dispose());
            return lifetime;
        }

        public Task<T> CreateNextFireTask()
        {
            var tcs = new TaskCompletionSource<T>();
            this.SubscribeOnce(args => SetResult(args, tcs));
            return tcs.Task;
        }

        private void SetResult(T args, object obj)
        {
            (obj as TaskCompletionSource<T>).SetResult(args);
        }
    }
}
