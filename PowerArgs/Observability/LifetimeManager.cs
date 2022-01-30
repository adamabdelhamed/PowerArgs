using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerArgs
{
    /// <summary>
    /// An interface that defined the contract for associating cleanup
    /// code with a lifetime
    /// </summary>
    public interface ILifetimeManager
    {
        /// <summary>
        /// Registers the given cleanup code to run when the lifetime being
        /// managed by this manager ends
        /// </summary>
        /// <param name="cleanupCode">the code to run</param>
        /// <returns>a Task that resolves after the cleanup code runs</returns>
        void OnDisposed(Action cleanupCode);

        /// <summary>
        /// Registers the given disposable to dispose when the lifetime being
        /// managed by this manager ends
        /// </summary>
        /// <param name="obj">the object to dispose</param>
        /// <returns>a Task that resolves after the object is disposed</returns>
        void OnDisposed(IDisposable obj);

        /// <summary>
        /// returns true if expired
        /// </summary>
        bool IsExpired { get;  }

        /// <summary>
        /// returns true if expiring
        /// </summary>
        bool IsExpiring { get; }
    }

    public static class ILifetimeManagerEx
    {
        /// <summary>
        /// Delays until this lifetime is complete
        /// </summary>
        /// <returns>an async task</returns>
        public static async Task AwaitEndOfLifetime(this ILifetimeManager manager)
        {
            while (manager != null && manager.IsExpired == false)
            {
                await Task.Yield();
            }
        }
    }

    /// <summary>
    /// An implementation of ILifetimeManager
    /// </summary>
    public class LifetimeManager : ILifetimeManager
    {
        internal List<Action> cleanupItems;
        internal List<IDisposable> cleanupItems2;

        /// <summary>
        /// returns true if expired
        /// </summary>
        public bool IsExpired { get; internal set; }
        public bool IsExpiring { get; internal set; }

        /// <summary>
        /// Creates the lifetime manager
        /// </summary>
        public LifetimeManager()
        {

        }

        /// <summary>
        /// Registers the given disposable to dispose when the lifetime being
        /// managed by this manager ends
        /// </summary>
        /// <param name="obj">the object to dispose</param>
        public void OnDisposed(IDisposable obj)
        {
            cleanupItems2 = cleanupItems2 ?? new List<IDisposable>();
            cleanupItems2.Add(obj);
        }

        /// <summary>
        /// Registers the given cleanup code to run when the lifetime being
        /// managed by this manager ends
        /// </summary>
        /// <param name="cleanupCode">the code to run</param>
        public void OnDisposed(Action cleanupCode)
        {
            cleanupItems = cleanupItems ?? new List<Action>();
            cleanupItems.Add(cleanupCode);
        }
    }
}
