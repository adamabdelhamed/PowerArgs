﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// Extension methods that make it easy to work with metadata collections
    /// </summary>
    public static class IArgMetadataEx
    {
        internal static List<T> AssertAreAllInstanceOf<T>(this IEnumerable<IArgMetadata> all)
        {
            var valid = from meta in all where meta is T == true select (T)meta;
            var invalid = from meta in all where meta is T == false select meta;

            foreach (var invalidMetadata in invalid)
            {
                throw new InvalidArgDefinitionException("Metadata of type '" + invalidMetadata.GetType().Name + "' does not implement " + typeof(T).Name);
            }

            return valid.ToList();
        }

        /// <summary>
        /// Returns true if the given collection of metadata contains metadata of the generic type T
        /// provided.
        /// </summary>
        /// <typeparam name="T">The type of metadata to search for</typeparam>
        /// <param name="metadata">The list of metadata to search</param>
        /// <returns>rue if the given collection of metadata contains metadata of the generic type T
        /// provided, otherwise false</returns>
        public static bool HasMeta<T>(this List<IArgMetadata> metadata) where T : class
        {
            foreach (var meta in metadata)
            {
                if (meta is T)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasMeta<TSearch,TCollectionType>(this List<TCollectionType> metadata) where TSearch : class where TCollectionType : IArgMetadata
        {
            foreach (var meta in metadata)
            {
                if (meta is TSearch)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the first instance of metadata of the given generic type T in the collection
        /// or null if it was not found.
        /// </summary>
        /// <typeparam name="T">The type of metadata to search for</typeparam>
        /// <param name="metadata">The list of metadata to search</param>
        /// <returns>the first instance of an metadata of the given generic type T in the collection
        /// or null if it was not found</returns>
        public static TSearch Meta<TSearch, TCollectionType>(this List<TCollectionType> metadata) where TSearch : class where TCollectionType : IArgMetadata
        {
            for (var i = 0; i < metadata.Count(); i++)
            {
                if (metadata[i] is TSearch ts)
                {
                    return ts;
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the first instance of metadata of the given generic type T in the collection.
        /// </summary>
        /// <typeparam name="T">The type of metadata to search for</typeparam>
        /// <param name="metadata">The list of metadata to search</param>
        /// <param name="ret">the our variable to set if the metadata was found</param>
        /// <returns>true if the metadata was found, otherwise false</returns>
        public static bool TryGetMeta<TSearch, TCollectionType>(this List<TCollectionType> metadata, out TSearch ret) where TSearch : class, IArgMetadata where TCollectionType : IArgMetadata
        {
            if (metadata.HasMeta<TSearch, TCollectionType>())
            {
                ret = metadata.Meta<TSearch, TCollectionType>();
                return true;
            }
            else
            {
                ret = null;
                return false;
            }
        }

        /// <summary>
        /// Gets the subset of metadata of the given generic type T from the collection.
        /// </summary>
        /// <typeparam name="T">The type of metadata to search for</typeparam>
        /// <param name="metadata">The list of metadata to search</param>
        /// <returns>the subset of metadata of the given generic type T from the collection</returns>
        //public static IEnumerable<T> Metas<T>(this IEnumerable<IArgMetadata> metadata) where T : class => metadata.OfType<T>();
    }
}
