using PowerArgs.Cli;
using System;
using System.Threading;

namespace PowerArgs
{
   

    public class BackgroundThread : Lifetime
    {
        private Action backgroundImpl;
        public BackgroundThread(Action backgroundImpl)
        {
            this.backgroundImpl = backgroundImpl;
        }

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
