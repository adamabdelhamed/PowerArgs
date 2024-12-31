using System;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs
{
    public static class ArrayEx
    {
        public static bool None<T>(this IEnumerable<T> items) => items.Any() == false;

        public static int RemoveWhere<T>(this IList<T> list, Func<T,bool> whereClause)
        {
            var toRemove = list.Where(item => whereClause(item)).ToList();
            var removed = 0;
            foreach(var item in toRemove)
            {
                if(list.Remove(item))
                {
                    removed++;
                }
            }
            return removed;
        }

        public static List<T> ToList<T>(this Array a)
        {
            List<T> ret = new List<T>();
            foreach(var element in a)
            {
                ret.Add((T)element);
            }
            return ret;
        }

        public static T MaxOrDefault<T>(this IEnumerable<T> src, T? defaultVal = default) where T : IComparable<T>
        {
            var max = defaultVal;
            foreach(var val in src)
            {
                max = val.CompareTo(max) > 0 ? val : max;
            }
            return max;
        }

        

        public static T MinOrDefault<T>(this IEnumerable<T> src, T defaultVal = default(T)) where T : IComparable<T>
        {
            var min = defaultVal;
            foreach (var val in src)
            {
                min = val.CompareTo(min) < 0 ? val : min;
            }
            return min;
        }

        public static IEnumerable<List<T>> ToBatchesOf<T>(this IEnumerable<T> items, int n)
        {
            var currentBatch = new List<T>();

            foreach(var item in items)
            {
                currentBatch.Add(item);
                if(currentBatch.Count == n)
                {
                    yield return currentBatch;
                    currentBatch = new List<T>();
                }
            }

            if(currentBatch.Count > 0)
            {
                yield return currentBatch;
            }
        }
        private static Random r = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var randomIndex = r.Next(0, list.Count);
                var temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}
