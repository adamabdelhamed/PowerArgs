using System;
using System.Collections.Generic;
using System.Threading;

namespace PowerArgs
{
    /// <summary>
    /// The callee portion of the promise abstraction
    /// </summary>
    public class Deferred
    {
        /// <summary>
        /// The promise to defer
        /// </summary>
        public Promise Promise { get; private set; }

        /// <summary>
        /// Returns true if the promise has been resolved
        /// </summary>
        public bool IsFulFilled { get; private set; }

        /// <summary>
        /// The exception associated with the deferred work, or null if there is none
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Handlers to call after the deferred work is complete
        /// </summary>
        public List<Action> Thens { get; private set; } 

        /// <summary>
        /// Handlers to call if the deferred work fails
        /// </summary>
        public List<Action<Exception>> Fails { get; private set; }

        /// <summary>
        /// used as the lock key when synchronizing work
        /// </summary>
        public object SyncObject { get; private set; }

        private Deferred()
        {
            SyncObject = new object();
            Thens = new List<Action>();
            Fails = new List<Action<Exception>>();
            Promise = new Promise(this);
        }

        /// <summary>
        /// Creates a deferred
        /// </summary>
        /// <returns></returns>
        public static Deferred Create()
        {
            return new Deferred();
        }

        /// <summary>
        /// Rejects the deferred work given an exception that has the details
        /// </summary>
        /// <param name="ex">the details of the rejection</param>
        public void Reject(Exception ex)
        {
            lock(SyncObject)
            {
                if (IsFulFilled) throw new InvalidOperationException("Already fulfilled");

                foreach (var action in Fails)
                {
                    action(ex);
                }
                Exception = ex;
                IsFulFilled = true;
            }
        }

        /// <summary>
        /// Mark the deferred work as completed and notifies all then handlers
        /// </summary>
        public void Resolve()
        {
            lock (SyncObject)
            {
                if (IsFulFilled) throw new InvalidOperationException("Already fulfilled");

                foreach (var action in Thens)
                {
                    action();
                }

                IsFulFilled = true;
            }
        }
    }

    /// <summary>
    /// An abstract protocol for handling async method calls that is decoupled from the actual
    /// async nature of the operation
    /// </summary>
    public class Promise
    {
        private Deferred myDeferred;
        internal Promise(Deferred deferred)
        {
            this.myDeferred = deferred;
        }

        /// <summary>
        /// Registers an action to be called after the promise is resolved successfully
        /// </summary>
        /// <param name="a">the action to run after the promise is resolved</param>
        /// <returns>this promise</returns>
        public Promise Then(Action a)
        {
            lock(myDeferred.SyncObject)
            {
                if (myDeferred.IsFulFilled && myDeferred.Exception == null)
                {
                    a();
                }
                else if (!myDeferred.IsFulFilled)
                {
                    myDeferred.Thens.Add(a);
                }
            }
            return this;
        }

        /// <summary>
        /// Registers an action to run if this promise is rejected.
        /// </summary>
        /// <param name="a">the exception handler</param>
        /// <returns>this promise</returns>
        public Promise Fail(Action<Exception> a)
        {
            lock(myDeferred.SyncObject)
            {
                if (myDeferred.IsFulFilled && myDeferred.Exception != null)
                {
                    a(myDeferred.Exception);
                }
                else if (!myDeferred.IsFulFilled)
                {
                    myDeferred.Fails.Add(a);
                }
            }
            return this;
        }

        /// <summary>
        /// Blocks the current thread until the promise is resolved or rejected
        /// </summary>
        public void Wait()
        {
            while (myDeferred.IsFulFilled == false)
            {
                Thread.Sleep(1);
            }
        }

    }
}
