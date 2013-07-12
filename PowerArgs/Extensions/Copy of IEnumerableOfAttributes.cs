using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// Some helpers for examining attirbute collections
    /// </summary>
    public static class IEnumerableOfAttributes
    {
        /// <summary>
        /// Returns true if the given collection of attributes contains an attribute of the generic type T
        /// provided.
        /// </summary>
        /// <typeparam name="T">The type of attribute to search for</typeparam>
        /// <param name="attributes">The list of attributes to search</param>
        /// <returns>rue if the given collection of attributes contains an attribute of the generic type T
        /// provided, otherwise false</returns>
        public static bool HasAttr<T>(this IEnumerable<Attribute> attributes) where T : Attribute
        {
            return Attrs<T>(attributes).Count > 0;
        }

        /// <summary>
        /// Gets the first instance of an attribute of the given generic type T in the collection
        /// or null if it was not found.
        /// </summary>
        /// <typeparam name="T">The type of attribute to search for</typeparam>
        /// <param name="attributes">The list of attributes to search</param>
        /// <returns>the first instance of an attribute of the given generic type T in the collection
        /// or null if it was not found</returns>
        public static T Attr<T>(this IEnumerable<Attribute> attributes) where T : Attribute
        {
            return Attrs<T>(attributes).FirstOrDefault();
        }

        /// <summary>
        /// Try to get the first instance of an attribute of the given generic type T in the collection.
        /// </summary>
        /// <typeparam name="T">The type of attribute to search for</typeparam>
        /// <param name="attributes">The list of attributes to search</param>
        /// <param name="ret">the our variable to set if the attribute was found</param>
        /// <returns>true if the attribute was found, otherwise false</returns>
        public static bool TryGetAttr<T>(this IEnumerable<Attribute> attributes, out T ret) where T : Attribute
        {
            if (attributes.HasAttr<T>())
            {
                ret = attributes.Attr<T>();
                return true;
            }
            else
            {
                ret = null;
                return false;
            }
        }

        /// <summary>
        /// Gets the subset of attributes of the given generic type T from the collection.
        /// </summary>
        /// <typeparam name="T">The type of attributes to search for</typeparam>
        /// <param name="attributes">The list of attributes to search</param>
        /// <returns>the subset of attributes of the given generic type T from the collection</returns>
        public static List<T> Attrs<T>(this IEnumerable<Attribute> attributes) where T : Attribute
        {
            var match = from a in attributes where a is T select a as T;
            return match.ToList();
        }
    }
}
