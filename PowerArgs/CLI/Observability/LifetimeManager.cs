using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    public class LifetimeManager
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

        public void Manage(IDisposable item)
        {
            _managedItems.Add(item);
        }
    }
}
