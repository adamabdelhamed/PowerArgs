using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public static class IEnumerableOfAttributes
    {
        public static bool HasAttr<T>(this IEnumerable<Attribute> attributes) where T : Attribute
        {
            return Attrs<T>(attributes).Count > 0;
        }

        public static T Attr<T>(this IEnumerable<Attribute> attributes) where T : Attribute
        {
            return Attrs<T>(attributes).FirstOrDefault();
        }

        public static List<T> Attrs<T>(this IEnumerable<Attribute> attributes) where T : Attribute
        {
            var match = from a in attributes where a is T select a as T;
            return match.ToList();
        }

    }
}
