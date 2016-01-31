using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
   public class LifetimeManager
    {
        [ThreadStatic]
        private static Stack<LifetimeManager> _ambientLifetimeManagers;

        private static Stack<LifetimeManager> AbientLifetimeManagers
        {
            get
            {
                if (_ambientLifetimeManagers == null) _ambientLifetimeManagers = new Stack<LifetimeManager>();
                return _ambientLifetimeManagers;
            }
        }

        internal static LifetimeManager AmbientLifetimeManager
        {
            get
            {
                if (AbientLifetimeManagers.Count == 0)
                {
                    throw new InvalidOperationException($"There is no ambient {nameof(LifetimeManager)}.  Either call {nameof(ObservableObject.SubscribeUnmanaged)} or provide a {nameof(LifetimeManager)}");
                }
                else
                {
                    return AbientLifetimeManagers.Peek();
                }
            }
        }

        internal static void PushAmbientLifetimeManaer(LifetimeManager manager)
        {
            AbientLifetimeManagers.Push(manager);
        }

        internal static void PopAmbientLifetimeManager()
        {
            AbientLifetimeManagers.Pop();
        }

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
