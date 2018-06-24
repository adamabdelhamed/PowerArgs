using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// Extension methods for collections
    /// </summary>
    public static class CollectionsEx
    {
        /// <summary>
        /// Executes the given action for each element in the collection
        /// </summary>
        /// <typeparam name="T">The type of items in the collection</typeparam>
        /// <param name="enumerable">the collection</param>
        /// <param name="a">the action to execute against each item</param>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> a)
        {
            foreach(var item in enumerable)
            {
                a(item);
            }
        }
    }
}
