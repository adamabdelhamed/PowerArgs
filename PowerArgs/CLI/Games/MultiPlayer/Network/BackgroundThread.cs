using PowerArgs.Cli;
using System;
using System.Threading;

namespace PowerArgs
{
    /// <summary>
    /// A wrapper around a thread that models the thread as a lifetime
    /// and offers a promise based call pattern
    /// </summary>
    public class BackgroundThread : Lifetime
    {
        private Action backgroundImpl;

        /// <summary>
        /// Creates a background thread given an implementation action
        /// </summary>
        /// <param name="backgroundImpl">the action to execute in the background</param>
        public BackgroundThread(Action backgroundImpl)
        {
            this.backgroundImpl = backgroundImpl;
        }


        /// <summary>
        /// Starts the background thread
        /// </summary>
        /// <returns>A promise that will resolve if the thread completes normally and will reject if an exception bubbles to the top</returns>
        public Promise Start()
        {
            var d = Deferred.Create();
            var t = new Thread(()=>
            {
                try
                {
                    backgroundImpl();
                    d.Resolve();
                }
                catch(Exception ex)
                {
                    d.Reject(ex);
                }
                finally
                {
                    this.Dispose();
                }
            });
            t.IsBackground = true;
            t.Start();
            return d.Promise;
        }
    }
}
