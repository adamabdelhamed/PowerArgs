using System;
using System.Collections.Generic;
using System.Linq;

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
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> a)
        {
            foreach(var item in enumerable)
            {
                a(item);
            }
            return enumerable;
        }

        /// <summary>
        /// Filters the collection to only those who are of type TOut and then narrows the IEnumerable type to TOut
        /// </summary>
        /// <typeparam name="TIn">The broad input type of elements</typeparam>
        /// <typeparam name="TOut">The type to narrow to</typeparam>
        /// <param name="enumerable">the collection to filter and narrow</param>
        /// <returns>the filtered and narrowed enumerable</returns>
        public static IEnumerable<TOut> WhereAs<TIn, TOut>(this IEnumerable<TIn> enumerable) => enumerable.Where(i => i is TOut).Select(i => FoolTheCompilerCast<TOut>(i));

        private static T FoolTheCompilerCast<T>(object o) => (T)o;
    }
}
