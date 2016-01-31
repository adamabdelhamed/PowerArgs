using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class AmbientLifetimeScope : IDisposable
    {
        public AmbientLifetimeScope(LifetimeManager manager)
        {
            LifetimeManager.PushAmbientLifetimeManaer(manager);
        }

        ~AmbientLifetimeScope()
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
                LifetimeManager.PopAmbientLifetimeManager();
            }
        }
    }
}
