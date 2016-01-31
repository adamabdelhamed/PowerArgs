using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class Lifetime : IDisposable
    {
        private LifetimeManager _manager;
        public LifetimeManager LifetimeManager
        {
            get
            {
                if (_manager == null) throw new ObjectDisposedException("The lifetime has expired");
                return _manager;
            }
            set
            {
                _manager = value;
            }
        }

        public Lifetime()
        {
            LifetimeManager = new LifetimeManager();
        }

        ~Lifetime()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var item in LifetimeManager.ManagedItems)
                {
                    item.Dispose();
                }
                LifetimeManager = null;
            }
        }
    }
}
