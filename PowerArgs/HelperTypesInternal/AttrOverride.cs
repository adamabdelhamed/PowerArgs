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

        internal T2 Get<T1, T2>(string propertyName, IEnumerable<IArgMetadata> attriibutes, Func<T1, T2> getter, T2 defaultValue = default(T2)) where T1 : Attribute
        {
            bool hasOverride = overrideValues.ContainsKey(propertyName);
            bool hasMatchingAttribute = attriibutes.HasMeta<T1>();

            object attributeVal = default(T2);
            object overrideVal = default(T2);

            if (hasOverride)
            {
                overrideVal = overrideValues[propertyName];
            }

            if (hasMatchingAttribute)
            {
                T1 attribute = attriibutes.Meta<T1>();
                attributeVal = getter(attribute);
            }

            bool overrideIsDifferentFromAttributeVal;

            if(overrideVal == null)
            {
                overrideIsDifferentFromAttributeVal = attributeVal != null;
            }
            else
            {
                overrideIsDifferentFromAttributeVal = overrideVal.Equals(attributeVal) == false;
            }

            if (hasOverride)
            {
                return (T2)overrideVal;
            }
            else if (hasMatchingAttribute)
            {
                return (T2)attributeVal;
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
