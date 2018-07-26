using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        public bool IsFulfilled { get; private set; }

        /// <summary>
        /// The exception associated with the deferred work, or null if there is none
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Handlers to call after the deferred work is complete
        /// </summary>
        internal List<Action> Thens { get; private set; }

        /// <summary>
        /// Handlers to call if the deferred work fails
        /// </summary>
        internal List<Action<Exception>> Fails { get; private set; }

        /// <summary>
        /// Handlers to call after all other handlers
        /// </summary>
        internal List<Action<Promise>> Finalies { get; private set; }

        /// <summary>
        /// used as the lock key when synchronizing work
        /// </summary>
        public object SyncObject { get; private set; }

        private Deferred()
        {
            SyncObject = new object();
            Thens = new List<Action>();
            Fails = new List<Action<Exception>>();
            Finalies = new List<Action<Promise>>();
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
            lock (SyncObject)
            {
                if (IsFulfilled) throw new InvalidOperationException("Already fulfilled");

                Exception = ex;

                foreach (var action in Fails)
                {
                    action(ex);
                }

                foreach (var action in Finalies)
                {
                    action(Promise);
                }

                IsFulfilled = true;
            }
        }

        /// <summary>
        /// Mark the deferred work as completed and notifies all then handlers
        /// </summary>
        public void Resolve()
        {
            lock (SyncObject)
            {
                if (IsFulfilled) throw new InvalidOperationException("Already fulfilled");

                foreach (var action in Thens)
                {
                    action();
                }

                foreach (var action in Finalies)
                {
                    action(Promise);
                }

                IsFulfilled = true;
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

        public Exception Exception
        {
            get
            {
                return myDeferred.Exception;
            }
        }

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
            lock (myDeferred.SyncObject)
            {
                if (myDeferred.IsFulfilled && myDeferred.Exception == null)
                {
                    a();
                }
                else if (!myDeferred.IsFulfilled)
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
            lock (myDeferred.SyncObject)
            {
                if (myDeferred.IsFulfilled && myDeferred.Exception != null)
                {
                    a(myDeferred.Exception);
                }
                else if (!myDeferred.IsFulfilled)
                {
                    myDeferred.Fails.Add(a);
                }
            }
            return this;
        }

        /// <summary>
        /// Registers an action to run at the end of all handlers.
        /// </summary>
        /// <param name="a">the  handler</param>
        /// <returns>this promise</returns>
        public Promise Finally(Action<Promise> a)
        {
            Then(() => { a(this); });
            Fail((pPrime) => { a(this); });
            return this;
        }

        /// <summary>
        /// Blocks the current thread until the promise is resolved or rejected
        /// </summary>
        public void Wait(int sleepTime = 1)
        {
            bool done = false;
            Finally((pPrime) => done = true);
            while (done == false)
            {
                Thread.Sleep(sleepTime);
            }

            if (this.Exception != null)
            {
                throw new PromiseWaitException(this.Exception);
            }
        }

        public async Task AsAwaitable()
        {
            while (myDeferred.IsFulfilled == false)
            {
                await Task.Delay(1);
            }

            if (myDeferred.Exception != null)
            {
                throw new PromiseWaitException(myDeferred.Exception);
            }
        }

        public static Promise WhenAll(List<Promise> others)
        {
            List<Exception> aggregateExceptions = new List<Exception>();
            Deferred outerDeferred = Deferred.Create();

            int waitCount = others.Count;
            foreach (var promise in others)
            {
                promise.Finally((p) =>
                {
                    if (p.Exception != null)
                    {
                        aggregateExceptions.Add(p.Exception);
                    }

                    var decrementResult = Interlocked.Decrement(ref waitCount);
                    if (decrementResult == 0)
                    {
                        if (aggregateExceptions.Count == 0)
                        {
                            outerDeferred.Resolve();
                        }
                        else
                        {
                            outerDeferred.Reject(new AggregateException(aggregateExceptions.ToArray()));
                        }
                    }
                });
            }

            return outerDeferred.Promise;
        }

        public static Promise WhenAnyFail(List<Promise> others)
        {
            Deferred outerDeferred = Deferred.Create();

            int waitCount = others.Count;
            foreach (var promise in others)
            {
                promise.Finally((p) =>
                {
                    lock (outerDeferred)
                    {
                        if (p.Exception != null && outerDeferred.IsFulfilled == false)
                        {
                            outerDeferred.Resolve();
                        }


                        var decrementResult = Interlocked.Decrement(ref waitCount);
                        if (decrementResult == 0 && outerDeferred.IsFulfilled == false)
                        {
                            outerDeferred.Reject(new Exception("None of the promises failed"));
                        }
                    }
                });
            }

            return outerDeferred.Promise;
        }
    }

    public class Deferred<T>
    {
        public Promise<T> Promise { get; private set; }
        public T Result { get; private set; }

        public Exception Exception
        {
            get
            {
                return innerDeferred.Exception;
            }
        }

        public bool IsFulfilled
        {
            get
            {
                return innerDeferred.IsFulfilled;
            }
        }

        private Deferred()
        {
            innerDeferred = Deferred.Create();
            Promise = new Promise<T>(this);
        }

        internal Deferred innerDeferred;

        public static Deferred<T> Create()
        {
            return new Deferred<T>();
        }

        public void Resolve(T result)
        {
            this.Result = result;
            innerDeferred.Resolve();
        }

        public void Reject(Exception ex)
        {
            innerDeferred.Reject(ex);
        }
    }

    public class Promise<T>
    {
        private Deferred<T> myDeferred;
        private Promise innerPromise;

        public Exception Exception => myDeferred.Exception;
        public T Result => myDeferred.Result;
        public void Wait() => innerPromise.Wait();

        internal Promise(Deferred<T> myDeferred)
        {
            this.myDeferred = myDeferred;
            this.innerPromise = new Promise(myDeferred.innerDeferred);
        }

        public Promise<T> Then(Action<T> thenHandler)
        {
            innerPromise.Then(() => { thenHandler(myDeferred.Result); });
            return this;
        }

        public Promise<T> Fail(Action<Exception> handler)
        {
            innerPromise.Fail(handler);
            return this;
        }

        public Promise<T> Finally(Action<Promise<T>> handler)
        {
            innerPromise.Finally((promise) => { handler(this); });
            return this;
        }

        public Task<T> AsAwaitable()
        {
            Func<Task<T>> ret = new Func<Task<T>>(async () =>
            {
                while (myDeferred.IsFulfilled == false)
                {
                    await Task.Delay(1);
                }

                if (myDeferred.Exception != null)
                {
                    throw new PromiseWaitException(myDeferred.Exception);
                }

                return Result;
            });
            return ret();
        }
    }

    /// <summary>
    /// An aggregate exception that is thrown from the Wait()
    /// method of a promise in the case where the promise fails.
    /// 
    /// Inner aggregate exceptions will be unwrapped so that the elements within 
    /// this exception's InnerExceptions property will never contain aggregate exceptions.
    /// </summary>
    public class PromiseWaitException : AggregateException
    {
        internal PromiseWaitException(Exception inner) : base("There were one or more exceptions that caused this promise to fail", Clean(inner)) { }

        private static List<Exception> Clean(Exception ex)
        {
            if (ex is AggregateException)
            {
                return Clean(((AggregateException)ex).InnerExceptions);
            }
            else
            {
                return new List<Exception>() { ex };
            }
        }

        private static List<Exception> Clean(IEnumerable<Exception> inners)
        {
            List<Exception> cleaned = new List<Exception>();
            foreach (var exception in inners)
            {
                if (exception is AggregateException)
                {
                    cleaned.AddRange(Clean(((AggregateException)exception).InnerExceptions));
                }
                else
                {
                    cleaned.Add(exception);
                }
            }

            return cleaned;
        }
    }
}
