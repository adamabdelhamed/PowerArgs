using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// Helpers for working with enums
    /// </summary>
    public static class Enums
    {
        /// <summary>
        /// Gets the values of an enum using generics to avoid casting
        /// </summary>
        /// <typeparam name="T">the enum type</typeparam>
        /// <returns>a list of enum values</returns>
        public static List<T> GetEnumValues<T>() => GetEnumValues(typeof(T)).Select(v => (T)v).ToList();

        /// <summary>
        /// Gets the values of an enum as a list
        /// </summary>
        /// <param name="t">the enum type</param>
        /// <returns>a list of enum values</returns>
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
