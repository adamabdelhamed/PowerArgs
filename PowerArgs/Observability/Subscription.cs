using System;

namespace PowerArgs
{
    public class Subscription : Disposable
    {
        protected Action unsubscribeHandler;
        public Subscription(Action unsubscribeHandler)
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
