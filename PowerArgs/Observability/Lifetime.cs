using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerArgs
{
    /// <summary>
    /// An object that has a beginning and and end  that can be used to define the lifespan of event and observable subscriptions.
    /// </summary>
    public class Lifetime : Disposable, ILifetimeManager
    {
        private LifetimeManager _manager;

        private static Lifetime forever = new Lifetime();

        /// <summary>
        /// The forever lifetime manager that will never end. Any subscriptions you intend to keep forever should use this lifetime so it's easy to spot leaks.
        /// </summary>
        public static LifetimeManager Forever => forever._manager;

        /// <summary>
        /// If true then this lifetime has already ended
        /// </summary>
        public bool IsExpired
        {
            get
            {
                return _manager == null;
            }
        }
        
        /// <summary>
        /// Creates a new lifetime
        /// </summary>
        public Lifetime()
        {
            _manager = new LifetimeManager();
        }

        /// <summary>
        /// Delays until this lifetime is complete
        /// </summary>
        /// <returns>an async task</returns>
        public async Task AwaitEndOfLifetime()
        {
            while(IsExpired == false)
            {
                await Task.Delay(10);
            }
        }

        /// <summary>
        /// Registers an action to run when this lifetime ends
        /// </summary>
        /// <param name="cleanupCode">code to run when this lifetime ends</param>
        /// <returns>a promis that will resolve after the cleanup code has run</returns>
        public Promise OnDisposed(Action cleanupCode)
        {
            if (IsExpired == false)
            {
                return _manager.OnDisposed(cleanupCode);
            }
            else
            {
                cleanupCode();
                var d = Deferred.Create();
                d.Resolve();
                return d.Promise;
            }
        }

        /// <summary>
        /// Registers a disposable to be disposed when this lifetime ends
        /// </summary>
        /// <param name="cleanupCode">an object to dispose when this lifetime ends</param>
        /// <returns>a promise that will resolve when the given object is disposed</returns>
        public Promise OnDisposed(IDisposable cleanupCode)
        {
            if (IsExpired == false)
            {
                return _manager.OnDisposed(cleanupCode);
            }
            else
            {
                cleanupCode.Dispose();
                var d = Deferred.Create();
                d.Resolve();
                return d.Promise;
            }
        }

        /// <summary>
        /// Creates a new lifetime that will end when any of the given
        /// lifetimes ends
        /// </summary>
        /// <param name="others">the lifetimes to use to generate this new lifetime</param>
        /// <returns>a new lifetime that will end when any of the given
        /// lifetimes ends</returns>
        public static Lifetime EarliestOf(params Lifetime[] others)
        {
            return EarliestOf((IEnumerable<Lifetime>)others);
        }

        /// <summary>
        /// Creates a new lifetime that will end when any of the given
        /// lifetimes ends
        /// </summary>
        /// <param name="others">the lifetimes to use to generate this new lifetime</param>
        /// <returns>a new lifetime that will end when any of the given
        /// lifetimes ends</returns>
        public static Lifetime EarliestOf(IEnumerable<Lifetime> others)
        {
            Lifetime ret = new Lifetime();
            foreach (var other in others)
            {
                other.OnDisposed(() =>
                {
                    if(ret.IsExpired == false)
                    {
                        ret.Dispose();
                    }
                });
            }
            return ret;
        }

        /// <summary>
        /// Creates a new lifetime that may be disposed earlier, but will be disposed when this
        /// lifetime ends
        /// </summary>
        /// <returns></returns>
        public Lifetime CreateChildLifetime()
        {
            var ret = new Lifetime();
            _manager.OnDisposed(()=>
            {
                if(ret.IsExpired == false)
                {
                    ret.Dispose();
                }
            });
            return ret;
        }

        /// <summary>
        /// Runs all the cleanup actions that have been registerd
        /// </summary>
        protected override void DisposeManagedResources()
        {
            if (!IsExpired)
            {
                foreach (var item in _manager.ManagedItems)
                {
                    item.Dispose();
                }
                _manager = null;
            }
        }
    }
}
