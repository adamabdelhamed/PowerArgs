using System;

namespace PowerArgs
{
    /// <summary>
    /// A utility that can be used to implement the following request pattern.  A user starts typing, triggering an async search.  
    /// While the async search is running, the user continues typing, triggering more searches.  Your goal is to ignore searches that
    /// are in flight, but are no longer the latest search.  This helper provides a programming model around orchestrating that behavior.
    /// </summary>
    internal class ExpireableAsyncRequestManager
    {
        private bool isExpired;
        private Guid latestRequestId;
        private object syncLock;

        /// <summary>
        /// Creates a new instance of the helper context
        /// </summary>
        public ExpireableAsyncRequestManager()
        {
            syncLock = new object();
            isExpired = false;
            latestRequestId = Guid.Empty;
        }

        /// <summary>
        /// You should call this from your foreground thread just before starting your async search request.  It will return a unique Id
        /// that you should make available to your code that runs when your async request completes.
        /// </summary>
        /// <returns>a unique Id
        /// that you should make available to your code that runs when your async request completes</returns>
        public Guid BeginRequest()
        {
            lock (syncLock)
            {
                latestRequestId = isExpired ? Guid.Empty : Guid.NewGuid();
                return latestRequestId;
            }
        }

        /// <summary>
        /// When your async call is complete, and you have your results, call this method, passing in the action that should
        /// only be run if two conditions are both true.  1 - The given request Id represents the latest request.  2 - Nobody has
        /// called ExpireAll() on the context.
        /// </summary>
        /// <param name="endAction">the action to invoke only if the context is not expired and the given request id represents the most recent request</param>
        /// <param name="requestId">The request Id that you got from BeginRequest </param>
        public void EndRequest(Action endAction, Guid requestId)
        {
            lock (syncLock)
            {
                if (isExpired == true || Guid.Empty == requestId)
                {
                    return;
                }

                if (requestId != latestRequestId)
                {
                    return;
                }

                endAction();
            }
        }

        /// <summary>
        /// Calling this method will disable end actions for the latest request as well as any future requests.  This is useful if your application
        /// have moved on to a different context and you want to ignore any pending requests.
        /// </summary>
        public void ExpireAll()
        {
            lock (syncLock)
            {
                isExpired = true;
            }
        }
    }
}
