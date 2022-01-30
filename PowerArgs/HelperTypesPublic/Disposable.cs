using System;

namespace PowerArgs
{
    /// <summary>
    /// A wrapper over IDisposable
    /// </summary>
    public abstract class Disposable : IDisposable
    {
        /// <summary>
        /// The deconstructor
        /// </summary>
        ~Disposable()
        {
            Dispose(false);
        }

        /// <summary>
        /// The dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void AfterDispose() { }

        /// <summary>
        /// The protected dispose method
        /// </summary>
        /// <param name="disposing">i used to know</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeManagedResources();
            }
        }

        /// <summary>
        /// Your real cleanup code goes here
        /// </summary>
        protected abstract void DisposeManagedResources();
    }

    /// <summary>
    /// An implementation of IDisposable that does nothing
    /// </summary>
    public class DummyDisposable : Disposable
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        protected override void DisposeManagedResources() { }
    }
}
