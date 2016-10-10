using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public static class ProxiedObservableExtensions
    {
        public static IDisposable SubscribeProxiedUnmanaged(this IObservableObject obj,ConsoleApp app, string propertyName, Action handler)
        {
            return obj.SubscribeUnmanaged(propertyName, () =>
            {
                app.QueueAction(handler);
            });
        }

        public static void SubscribeProxiedForLifetime(this IObservableObject obj, ConsoleApp app, string propertyName, Action handler, LifetimeManager manager)
        {
            obj.SubscribeForLifetime(propertyName, () =>
             {
                 app.QueueAction(handler);
             }, manager);
        }

        public static IDisposable SynchronizeProxiedUnmanaged(this IObservableObject obj, ConsoleApp app, string propertyName, Action handler)
        {
            return obj.SynchronizeUnmanaged(propertyName, () =>
            {
                app.QueueAction(handler);
            });
        }

        public static void SynchronizeProxiedForLifetime(this IObservableObject obj, ConsoleApp app, string propertyName, Action handler, LifetimeManager manager)
        {
            obj.SynchronizeForLifetime(propertyName, () =>
            {
                app.QueueAction(handler);
            }, manager);
        }
    }
}
