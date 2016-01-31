using System;

namespace PowerArgs.Cli
{
    public class Subscription : Disposable
    {
        protected Action unsubscribeHandler;
        public Subscription(Action unsubscribeHandler)
        {
            this.unsubscribeHandler = unsubscribeHandler;
        }

        protected Subscription()
        {

        }

        protected override void DisposeManagedResources()
        {
            if (unsubscribeHandler != null)
            {
                unsubscribeHandler();
            }
        }
    }
}
