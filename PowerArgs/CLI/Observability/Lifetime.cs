using System;
using System.Collections.Generic;

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

        public static Lifetime EarliestOf(params Lifetime[] others)
        {
            return EarliestOf((IEnumerable<Lifetime>)others);
        }

        public static Lifetime EarliestOf(IEnumerable<Lifetime> others)
        {
            Lifetime ret = new Lifetime();
            foreach (var other in others)
            {
                other.LifetimeManager.Manage(new Subscription(() =>
                {
                    if(ret.IsExpired == false)
                    {
                        ret.Dispose();
                    }
                }));
            }
            return ret;
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
