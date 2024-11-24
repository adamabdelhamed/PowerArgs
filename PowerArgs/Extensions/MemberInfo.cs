using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// Provides some reflection helpers in the form of extension methods for the MemberInfo type.
    /// </summary>
    public static class MemberInfoEx
    {
        /// <summary>
        /// A cache of all attributes that have been queried via reflection.
        /// </summary>
        public static readonly Dictionary<(MemberInfo Member, Type AttributeType), object> CachedAttributes = new();

        /// <summary>
        /// Returns true if the given member has an attribute of the given type (including inherited types).
        /// </summary>
        public static bool HasAttr<T>(this MemberInfo info)
        {
            return info.Attrs<T>().Count > 0;
        }

        /// <summary>
        /// Gets the attribute of the given type or null if the member does not have this attribute defined.
        /// </summary>
        public static T Attr<T>(this MemberInfo info)
        {
            if (info.HasAttr<T>())
            {
                return info.Attrs<T>()[0];
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Gets the attributes of the given type. Caches results to avoid repeated allocations.
        /// </summary>
        public static List<T> Attrs<T>(this MemberInfo info)
        {
            var cacheKey = (Member: info, AttributeType: typeof(T));

            lock (CachedAttributes)
            {
                if (CachedAttributes.TryGetValue(cacheKey, out var cachedValue))
                {
                    return (List<T>)cachedValue;
                }

                var freshValue = info
                    .GetCustomAttributes(true)
                    .OfType<T>()
                    .ToList();

                CachedAttributes[cacheKey] = freshValue;
                return freshValue;
            }
        }
    }
}
