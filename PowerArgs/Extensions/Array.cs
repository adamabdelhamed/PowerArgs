using System;
using System.Collections.Generic;

namespace PowerArgs
{
    public static class ArrayEx
    {
        public static List<T> ToList<T>(this Array a)
        {
            List<T> ret = new List<T>();
            foreach(var element in a)
            {
                ret.Add((T)element);
            }
            return ret;
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
    }
}
