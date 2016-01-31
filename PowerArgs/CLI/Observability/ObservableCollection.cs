using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    /// <summary>
    /// An observable list implementation
    /// </summary>
    /// <typeparam name="T">the type of elements this collection will contain</typeparam>
    public class ObservableCollection<T> : IList<T>
    {
        public string Id { get; set; } 

        /// <summary>
        /// Called before an item is added to the list
        /// </summary>
        public Event<T> BeforeAdded { get; private set; } = new Event<T>();

        /// <summary>
        /// Called after an item is removed from the list
        /// </summary>
        public Event<T> BeforeRemoved { get; private set; } = new Event<T>();

        /// <summary>
        /// Called when an element is added to this list
        /// </summary>
        public Event<T> Added { get; private set; } = new Event<T>();

        /// <summary>
        /// Called when an element is removed from this list
        /// </summary>
        public Event<T> Removed { get; private set; } = new Event<T>();

        /// <summary>
        /// Called whenever this list changes.  You may receive one event for multiple changes
        /// if the changes were atomic (e.g. after calling Clear()).
        /// </summary>
        public Event Changed { get; private set; } = new Event();

        private List<T> wrapped;

        Dictionary<T, Lifetime> membershipLifetimes;

        /// <summary>
        /// Initialized the collection
        /// </summary>
        public ObservableCollection()
        {
            wrapped = new List<T>();
            membershipLifetimes = new Dictionary<T, Lifetime>();
        }

        public void SynchronizeForLifetime(Action<T> addAction, Action<T> removeAction, Action changedAction, LifetimeManager manager)
        {
            Added.SubscribeForLifetime(addAction, manager);
            Removed.SubscribeForLifetime(removeAction, manager);
            Changed.SubscribeForLifetime(changedAction, manager);

            foreach (var obj in this)
            {
                addAction(obj);
            }

            changedAction();
        }

        public void Synchronize(Action<T> addAction, Action<T> removeAction, Action changedAction)
        {
            SynchronizeForLifetime(addAction, removeAction, changedAction, LifetimeManager.AmbientLifetimeManager);
        }
        
        /// <summary>
        /// Fires the Added event for the given item
        /// </summary>
        /// <param name="item">The item that was added</param>
        internal void FireAdded(T item)
        {
            membershipLifetimes.Add(item, new Lifetime());
            Added.Fire(item);
            Changed.Fire();
        }

        /// <summary>
        /// Fired the Removed event for the given item
        /// </summary>
        /// <param name="item">The item that was removed</param>
        internal void FireRemoved(T item)
        {
            Removed.Fire(item);
            Changed.Fire();
            var itemLifetime = membershipLifetimes[item];
            membershipLifetimes.Remove(item);
            itemLifetime.Dispose();
        }

        public LifetimeManager GetMembershipLifetime(T item)
        {
            return membershipLifetimes[item].LifetimeManager;
        }

        internal void FireBeforeAdded(T item)
        {
            BeforeAdded.Fire(item);
        }



        internal void FireBeforeRemoved(T item)
        {
            BeforeRemoved.Fire(item);
        }

        /// <summary>
        /// Returns the index of the given item in the list
        /// </summary>
        /// <param name="item">the item to look for</param>
        /// <returns>the index or a negative number if the element is not in the list</returns>
        public int IndexOf(T item)
        {
            return wrapped.IndexOf(item);
        }

        /// <summary>
        /// Inserts the given item into the list at the specified position
        /// </summary>
        /// <param name="index">the index to insert the item into</param>
        /// <param name="item">the item to insert</param>
        public void Insert(int index, T item)
        {
            FireBeforeAdded(item);
            wrapped.Insert(index, item);
            FireAdded(item);
        }

        /// <summary>
        /// Removes the element at the specified index
        /// </summary>
        /// <param name="index">the index of the item to remove</param>
        public void RemoveAt(int index)
        {
            var item = wrapped[index];
            FireBeforeRemoved(item);
            wrapped.RemoveAt(index);
            FireRemoved(item);
        }

        /// <summary>
        /// Gets or sets the value at a particular index
        /// </summary>
        /// <param name="index">the index of the item to get or set</param>
        /// <returns>the value at a particular index</returns>
        public T this[int index]
        {
            get
            {
                return wrapped[index];
            }
            set
            {
                var item = wrapped[index];

                FireBeforeRemoved(item);
                FireBeforeAdded(value);
                wrapped[index] = value;

                FireRemoved(item);
                FireAdded(value);
            }
        }

        /// <summary>
        /// Adds the given item to the list
        /// </summary>
        /// <param name="item">the item to add</param>
        public void Add(T item)
        {
            FireBeforeAdded(item);
            wrapped.Add(item);
            FireAdded(item);
        }

        /// <summary>
        /// Removes all items from the collection
        /// </summary>
        public void Clear()
        {
            if (Removed.HasSubscriptions)
            {
                var items = wrapped.ToArray();
                wrapped.Clear();

                foreach (var item in items)
                {
                    Removed.Fire(item);
                }
            }
            else
            {
                wrapped.Clear();
            }
        }

        /// <summary>
        /// Tests to see if the list contains the given item
        /// </summary>
        /// <param name="item">the item to look for</param>
        /// <returns>true if the list contains the given item, false otherwise</returns>
        public bool Contains(T item)
        {
            return wrapped.Contains(item);
        }

        /// <summary>
        /// Copies values from this list into the given array starting at the given index in the destination
        /// </summary>
        /// <param name="array">the destination array</param>
        /// <param name="arrayIndex">the index in the destination array to start the copy</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            wrapped.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of items in the list
        /// </summary>
        public int Count
        {
            get { return wrapped.Count; }
        }

        /// <summary>
        /// Always returns false
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the given item from the list
        /// </summary>
        /// <param name="item">the item to remove</param>
        /// <returns>true if an item was removed, false if the item was not found in the list</returns>
        public bool Remove(T item)
        {
            if (wrapped.Contains(item))
            {
                FireBeforeRemoved(item);
                wrapped.Remove(item);
                FireRemoved(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets an enumerator for this list
        /// </summary>
        /// <returns>an enumerator for this list</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return wrapped.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator for this list
        /// </summary>
        /// <returns>an enumerator for this list</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return wrapped.GetEnumerator();
        }
    }
}
