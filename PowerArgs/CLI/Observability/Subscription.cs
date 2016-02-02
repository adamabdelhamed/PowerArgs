using System;

namespace PowerArgs.Cli
{
    public class Subscription : Disposable
    {
        protected Action unsubscribeHandler;
        internal Subscription(Action unsubscribeHandler)
        {
            this.unsubscribeHandler = unsubscribeHandler;
        }

        internal Subscription()
        {

        }

        protected override void DisposeManagedResources()
        {
            if (unsubscribeHandler != null)
            {
                unsubscribeHandler();
                unsubscribeHandler = null;
            }
        }
    }
}
