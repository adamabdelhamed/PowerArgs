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
        /// <summary>
        /// Called when an element is added to this list
        /// </summary>
        public event Action<T> Added;

        /// <summary>
        /// Called when an element is removed from this list
        /// </summary>
        public event Action<T> Removed;

        List<T> wrapped;

        /// <summary>
        /// Initialized the collection
        /// </summary>
        public ObservableCollection()
        {
            wrapped = new List<T>();
        }

        /// <summary>
        /// Fires the Added event for the given item
        /// </summary>
        /// <param name="item">The item that was added</param>
        internal void FireAdded(T item)
        {
            if (Added != null) Added(item);
        }

        /// <summary>
        /// Fired the Removed event for the given item
        /// </summary>
        /// <param name="item">The item that was removed</param>
        internal void FireRemoved(T item)
        {
            if (Removed != null) Removed(item);
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
            wrapped.Add(item);
            FireAdded(item);
        }

        /// <summary>
        /// Removes all items from the collection
        /// </summary>
        public void Clear()
        {
            var items = wrapped.ToArray();
            wrapped.Clear();
            if (Removed != null)
            {
                foreach (var item in items)
                {
                    Removed(item);
                }
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
            if (wrapped.Remove(item))
            {
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
