using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    internal static class ArrayEx
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
    }
}
