using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
   public class LifetimeManager
    { 
         
        internal List<IDisposable> ManagedItems { get; private set; }

        public LifetimeManager()
        {
            ManagedItems = new List<IDisposable>();
        }

        public void Manage(IDisposable item)
        {
            ManagedItems.Add(item);
        }
    }
}
