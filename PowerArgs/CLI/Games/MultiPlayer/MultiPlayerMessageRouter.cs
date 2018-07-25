using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Games
{
    public class MultiPlayerMessageRouter
    {
        private EventRouter<MultiPlayerMessage> innerRouter;
        public Event<MultiPlayerMessage> NotFound { get; private set; } = new Event<MultiPlayerMessage>();
        IDisposable sub;
        public MultiPlayerMessageRouter()
        {
            innerRouter = new EventRouter<MultiPlayerMessage>();
            sub = innerRouter.NotFound.SubscribeUnmanaged((m) => NotFound.Fire(m.Data));
        }

  

        public void Route(string messageTypeName, MultiPlayerMessage message) => innerRouter.Route(messageTypeName, message);

        public void Register<T>(Action<T> handler, ILifetimeManager lifetimeManager) where T : MultiPlayerMessage
        {
            innerRouter.Register(typeof(T).Name, (message) => handler((T)message.Data), lifetimeManager);
        }

        public void RegisterOnce<T>(Action<T> handler) where T : MultiPlayerMessage
        {
            innerRouter.RegisterOnce(typeof(T).Name, (message) => handler((T)message.Data));
        }

        public async Task<T> Await<T>(TimeSpan? timeout = null) where T : MultiPlayerMessage
        {
            var innerResult = await innerRouter.Await(typeof(T).Name, timeout);
            return (T)innerResult.Data;
        }
    }
}
