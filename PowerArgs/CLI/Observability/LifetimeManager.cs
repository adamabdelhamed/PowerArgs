using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    public interface ILifetimeManager
    {
        Promise OnDisposed(Action cleanupCode);
        Promise OnDisposed(IDisposable cleanupCode);
    }

    public class LifetimeManager : ILifetimeManager
    {
        private List<IDisposable> _managedItems;

        internal IReadOnlyCollection<IDisposable> ManagedItems
        {
            get
            {
                return _managedItems.AsReadOnly();
            }
        }

        public LifetimeManager()
        {
            _managedItems = new List<IDisposable>();
        }

        public Promise OnDisposed(IDisposable item) => OnDisposed(() => item.Dispose());

        public Promise OnDisposed(Action cleanupCode)
        {
            var d = Deferred.Create();
            _managedItems.Add(new Subscription(()=>
            {
                cleanupCode();
                d.Resolve();
            }));
            return d.Promise;
        }
    }
}
