using System;
using System.Collections.Generic;

namespace PowerArgs
{
    internal class AttrOverride
    {
        Dictionary<string, object> overrideValues;

        Type hostingType;

        public AttrOverride(Type hostingType)
        {
            this.hostingType = hostingType;
            overrideValues = new Dictionary<string, object>();
        }

        internal void Set(string propertyName, object value)
        {
            if (overrideValues.ContainsKey(propertyName))
            {
                overrideValues[propertyName] = value;
            }
            else
            {
                overrideValues.Add(propertyName, value);
            }
        }

        internal T2 GetStruct<T1, T2>(string propertyName, IEnumerable<IArgMetadata> attributes, Func<T1, T2> getter, T2 defaultValue = default)
            where T1 : Attribute
            where T2 : struct // Constraint for value types
        {
            bool hasOverride = overrideValues.ContainsKey(propertyName);
            bool hasMatchingAttribute = attributes.HasMeta<T1>();

            T2 attributeVal = defaultValue;
            T2 overrideVal = defaultValue;

            if (hasOverride)
            {
                overrideVal = (T2)overrideValues[propertyName];
            }

            if (hasMatchingAttribute)
            {
                T1 attribute = attributes.Meta<T1>();
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

        internal T2 Get<T1, T2>(string propertyName, IEnumerable<IArgMetadata> attributes, Func<T1, T2> getter, T2 defaultValue = default)
    where T1 : Attribute
    where T2 : class // Constraint for reference types
        {
            bool hasOverride = overrideValues.ContainsKey(propertyName);
            bool hasMatchingAttribute = attributes.HasMeta<T1>();

            T2 attributeVal = defaultValue;
            T2 overrideVal = defaultValue;

            if (hasOverride)
            {
                overrideVal = (T2)overrideValues[propertyName];
            }

            if (hasMatchingAttribute)
            {
                T1 attribute = attributes.Meta<T1>();
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
