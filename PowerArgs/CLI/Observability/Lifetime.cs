using System;

namespace PowerArgs.Cli
{
    public class Lifetime : Disposable
    {
        private LifetimeManager _manager;

        public bool IsExpired
        {
            get
            {
                return _manager == null;
            }
        }

        public LifetimeManager LifetimeManager
        {
            get
            {
                if (_manager == null)
                {
                    throw new ObjectDisposedException("The lifetime has expired");
                }
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

        public Lifetime CreateChildLifetime()
        {
            var ret = new Lifetime();
            LifetimeManager.Manage(new Subscription(()=>
            {
                if(ret.IsExpired == false)
                {
                    ret.Dispose();
                }
            }));
            return ret;
        }

        protected override void DisposeManagedResources()
        {
            foreach (var item in LifetimeManager.ManagedItems)
            {
                item.Dispose();
            }
            LifetimeManager = null;
        }
    }
}
