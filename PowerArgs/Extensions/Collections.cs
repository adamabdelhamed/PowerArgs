using System.Collections;

namespace PowerArgs
{
    /// <summary>
    /// Extension methods for collections
    /// </summary>
    public static class CollectionsEx
    {

        /// <summary>
        /// Tests to see if the collection is null or empty
        /// </summary>
        /// <typeparam name="T">the type of element within the collection</typeparam>
        /// <param name="collection">the collection to test</param>
        /// <returns>true if the collection is null or empty, false otherwise</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection) => collection == null || collection.Count() == 0;

        /// <summary>
        /// Tests to see if the array is null or empty
        /// </summary>
        /// <typeparam name="T">the type of element within the array</typeparam>
        /// <param name="collection">the array to test</param>
        /// <returns>true if the array is null or empty, false otherwise</returns>
        public static bool IsNullOrEmpty<T>(this T[] array) => array == null || array.Length == 0;

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
        /// <typeparam name="TOut">The type to narrow to</typeparam>
        /// <param name="enumerable">the collection to filter and narrow</param>
        /// <returns>the filtered and narrowed enumerable</returns>
        public static IEnumerable<TOut> WhereAs<TOut>(this IEnumerable enumerable)
        {
            foreach(var item in enumerable)
            {
                if(item is TOut)
                {
                    yield return (TOut)item;
                }
            }
        }

        /// <summary>
        /// Filters the collection to only those who are not of type TOut and then narrows the IEnumerable type to TOut
        /// </summary>
        /// <typeparam name="TOut">The type to narrow to</typeparam>
        /// <param name="enumerable">the collection to filter and narrow</param>
        /// <returns>the filtered and narrowed enumerable</returns>
        public static IEnumerable<TOut> WhereAsNot<TNot, TOut>(this IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is TNot == false)
                {
                    yield return (TOut)item;
                }
            }
        }

        /// <summary>
        ///  narrows the IEnumerable type to TOut
        /// </summary>
        /// <typeparam name="TOut">The type to narrow to</typeparam>
        /// <param name="enumerable">the collection to narrow</param>
        /// <returns>the narrowed enumerable</returns>
        public static IEnumerable<TOut> As<TOut>(this IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                 yield return (TOut)item;
            }
        }

        /// <summary>
        ///  narrows the IEnumerable type to TOut
        /// </summary>
        /// <typeparam name="TOut">The type to narrow to</typeparam>
        /// <param name="enumerable">the collection to narrow</param>
        /// <returns>the narrowed enumerable</returns>
        public static List<TOut> AsList<TOut>(this IEnumerable enumerable)
        {
            var ret = new List<TOut>();
            foreach (var item in enumerable)
            {
                ret.Add((TOut)item);
            }
            return ret;
        }

        private static T FoolTheCompilerCast<T>(object o) => (T)o;
    }
}
