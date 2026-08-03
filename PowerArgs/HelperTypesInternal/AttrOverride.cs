namespace PowerArgs
{
    internal class AttrOverride
    {
        Dictionary<string, object> overrideValues;

        public AttrOverride(Type hostingType)
        {
        }

        internal void Set(string propertyName, object value)
        {
            overrideValues ??= new Dictionary<string, object>();
            overrideValues[propertyName] = value;
        }

        internal T2 GetStruct<T1, T2, TCollectionType>(string propertyName, List<TCollectionType> attributes, Func<T1, T2> getter, T2 defaultValue = default)
            where T1 : Attribute
            where T2 : struct // Constraint for value types
            where TCollectionType : IArgMetadata
        {
            object rawOverride = null;
            bool hasOverride = overrideValues != null && overrideValues.TryGetValue(propertyName, out rawOverride);
            bool hasMatchingAttribute = attributes.HasMeta<T1, TCollectionType>();

            T2 attributeVal = defaultValue;
            T2 overrideVal = defaultValue;

            if (hasOverride)
            {
                overrideVal = (T2)rawOverride;
            }

            if (hasMatchingAttribute)
            {
                T1 attribute = attributes.Meta<T1,TCollectionType>();
                attributeVal = getter(attribute);
            }

            if (hasOverride)
            {
                return overrideVal;
            }
            else if (hasMatchingAttribute)
            {
                return attributeVal;
            }
            else
            {
                return defaultValue;
            }
        }

        internal T2 Get<T1, T2, TCollectionType>(string propertyName, List<TCollectionType> attributes, Func<T1, T2> getter, T2 defaultValue = default)
    where T1 : Attribute
    where T2 : class // Constraint for reference types
    where TCollectionType : IArgMetadata
        {
            object rawOverride = null;
            bool hasOverride = overrideValues != null && overrideValues.TryGetValue(propertyName, out rawOverride);
            bool hasMatchingAttribute = attributes.HasMeta<T1,TCollectionType>();

            T2 attributeVal = defaultValue;
            T2 overrideVal = defaultValue;

            if (hasOverride)
            {
                overrideVal = (T2)rawOverride;
            }

            if (hasMatchingAttribute)
            {
                T1 attribute = attributes.Meta<T1, TCollectionType>();
                attributeVal = getter(attribute);
            }

            if (hasOverride)
            {
                return overrideVal;
            }
            else if (hasMatchingAttribute)
            {
                return attributeVal;
            }
            else
            {
                return defaultValue;
            }
        }


    }
}
