using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace PowerArgs
{
    public interface ILifetime : ILifetimeManager, IDisposable
    {
        bool TryDispose();
        void Dispose();
    }

    public static class ILifetimeEx
    {
        public static Lifetime CreateChildLifetime(this ILifetime lt)
        {
            var ret = new Lifetime();
            lt.OnDisposed(() =>
            {
                if (ret.IsExpired == false)
                {
                    ret.Dispose();
                }
            });
            return ret;
        }
    }



    /// <summary>
    /// An object that has a beginning and and end  that can be used to define the lifespan of event and observable subscriptions.
    /// </summary>
    public class Lifetime : Disposable, ILifetime
    {
        private LifetimeManager _manager;

        public LifetimeManager Manager => _manager;

        private static Lifetime forever = CreateForeverLifetime();

        private static Lifetime CreateForeverLifetime()
        {
            var ret = new Lifetime();

            ret.OnDisposed(() =>
            {
                throw new Exception("Forever lifetime expired");
            });

            return ret;
        }

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
        /// returns true if the lifetime's Dispose() method is currently running, false otherwise
        /// </summary>
        public bool IsExpiring { get; private set; }
        
        /// <summary>
        /// Creates a new lifetime
        /// </summary>
        public Lifetime()
        {
            _manager = new LifetimeManager();
            OnDisposed(() => _manager.IsExpired = true);
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
        public void OnDisposed(Action cleanupCode)
        {
            if (IsExpired == false)
            {
                _manager.OnDisposed(cleanupCode);
            }
        }

        /// <summary>
        /// Registers a disposable to be disposed when this lifetime ends
        /// </summary>
        /// <param name="cleanupCode">an object to dispose when this lifetime ends</param>
        public void OnDisposed(IDisposable cleanupCode)
        {
            if (IsExpired == false)
            {
                _manager.OnDisposed(cleanupCode);
            }
        }

        public bool TryDispose()
        {
            if(IsExpired || IsExpiring)
            {
                return false;
            }
            else
            {
                Dispose();
                return true;
            }
        }

        /// <summary>
        /// Creates a new lifetime that will end when any of the given
        /// lifetimes ends
        /// </summary>
        /// <param name="others">the lifetimes to use to generate this new lifetime</param>
        /// <returns>a new lifetime that will end when any of the given
        /// lifetimes ends</returns>
        public static Lifetime EarliestOf(params ILifetimeManager[] others)
        {
            return EarliestOf((IEnumerable<ILifetimeManager>)others);
        }

        /// <summary>
        /// Creates a new lifetime that will end when all of the given lifetimes end
        /// </summary>
        /// <param name="others">the lifetimes to use to generate this new lifetime</param>
        /// <returns>a new lifetime that will end when all of the given lifetimes end</returns>
        public static Lifetime WhenAll(params ILifetimeManager[] others)
        {
            var togo = others.Length;
            var ret = new Lifetime();
            foreach (var lifetime in others)
            {
                lifetime.OnDisposed(() =>
                {
                    if(Interlocked.Decrement(ref togo) == 0)
                    {
                        ret.Dispose();
                    }
                });
            }

            return ret;
        }

        /// <summary>
        /// Creates a new lifetime that will end when any of the given
        /// lifetimes ends
        /// </summary>
        /// <param name="others">the lifetimes to use to generate this new lifetime</param>
        /// <returns>a new lifetime that will end when any of the given
        /// lifetimes ends</returns>
        public static Lifetime EarliestOf(IEnumerable<ILifetimeManager> others)
        {
            Lifetime ret = new Lifetime();
            var count = 0;
            foreach (var other in others)
            {
                if (other != null)
                {
                    count++;
                    other.OnDisposed(() =>
                    {
                        if (ret.IsExpired == false)
                        {
                            ret.Dispose();
                        }
                    });
                }
            }
            if(count == 0)
            {
                ret.Dispose();
            }
            return ret;
        }

      

        /// <summary>
        /// Runs all the cleanup actions that have been registerd
        /// </summary>
        protected override void DisposeManagedResources()
        {
            if (!IsExpired)
            {
                IsExpiring = true;
                _manager.IsExpiring = true;
                try
                {
                    foreach (var item in _manager.cleanupItems.ToArray())
                    {
                        item();
                    }
                    _manager = null;
                }
                finally
                {
                    IsExpiring = false;
                }
            }
        }
    }
}
