using PowerArgs.Cli;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs
{
    /// <summary>
    /// A wrapper around a thread that models the thread as a lifetime
    /// and offers a Task based call pattern
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
        /// <returns>A Task that will resolve if the thread completes normally and will reject if an exception bubbles to the top</returns>
        public Task Start()
        {
            var d = new TaskCompletionSource<bool>();
            var t = new Thread(()=>
            {
                try
                {
                    backgroundImpl();
                    d.SetResult(true);
                }
                catch(Exception ex)
                {
                    d.SetException(ex);
                }
                finally
                {
                    this.Dispose();
                }
            });
            t.IsBackground = true;
            t.Start();
            return d.Task;
        }
    }
}
