using System;
using System.Collections.Generic;
using System.Text;

namespace PowerArgs
{
    public static class Enums
    {
        public static List<T> GetEnumValues<T>()
        {
            List<T> ret = new List<T>();
            foreach (T val in Enum.GetValues(typeof(T)))
            {
                ret.Add(val);
            }
            return ret;
        }

        public static List<object> GetEnumValues(Type t)
        {
            List<object> ret = new List<object>();
            foreach (object val in Enum.GetValues(t))
            {
                ret.Add(val);
            }
            return ret;
        }
    }
}
