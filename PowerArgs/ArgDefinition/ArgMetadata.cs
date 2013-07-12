using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// Any attribute that's purpose is to add information about a command line arguments definiton should
    /// derive from this type.  
    /// </summary>
    public interface IArgMetadata { }

    public interface ICommandLineArgumentMetadata : IArgMetadata { }
    public interface ICommandLineActionMetadata : IArgMetadata { }
    public interface ICommandLineArgumentsDefinitionMetadata : IArgMetadata { }

    public interface IGlobalArgMetadata : ICommandLineArgumentMetadata, ICommandLineActionMetadata, ICommandLineArgumentsDefinitionMetadata { }
    public interface IArgumentOrActionMetadata : ICommandLineActionMetadata, ICommandLineArgumentMetadata { }

    public static class IArgMetadataEx
    {
        /// <summary>
        /// Returns true if the given collection of attributes contains an attribute of the generic type T
        /// provided.
        /// </summary>
        /// <typeparam name="T">The type of attribute to search for</typeparam>
        /// <param name="attributes">The list of attributes to search</param>
        /// <returns>rue if the given collection of attributes contains an attribute of the generic type T
        /// provided, otherwise false</returns>
        public static bool HasMeta<T>(this IEnumerable<IArgMetadata> attributes) where T : Attribute
        {
            return Metas<T>(attributes).Count > 0;
        }

        /// <summary>
        /// Gets the first instance of an attribute of the given generic type T in the collection
        /// or null if it was not found.
        /// </summary>
        /// <typeparam name="T">The type of attribute to search for</typeparam>
        /// <param name="attributes">The list of attributes to search</param>
        /// <returns>the first instance of an attribute of the given generic type T in the collection
        /// or null if it was not found</returns>
        public static T Meta<T>(this IEnumerable<IArgMetadata> attributes) where T : Attribute
        {
            return Metas<T>(attributes).FirstOrDefault();
        }

        /// <summary>
        /// Try to get the first instance of an attribute of the given generic type T in the collection.
        /// </summary>
        /// <typeparam name="T">The type of attribute to search for</typeparam>
        /// <param name="attributes">The list of attributes to search</param>
        /// <param name="ret">the our variable to set if the attribute was found</param>
        /// <returns>true if the attribute was found, otherwise false</returns>
        public static bool TryGetMeta<T>(this IEnumerable<IArgMetadata> attributes, out T ret) where T : Attribute
        {
            if (attributes.HasMeta<T>())
            {
                ret = attributes.Meta<T>();
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
        public static List<T> Metas<T>(this IEnumerable<IArgMetadata> attributes) where T : Attribute
        {
            var match = from a in attributes where a is T select a as T;
            return match.ToList();
        }
    }
}
