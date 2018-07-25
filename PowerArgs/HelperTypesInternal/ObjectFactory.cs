using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace PowerArgs
{
    internal static class ObjectFactory
    {
        private static Dictionary<Type, Func<object>> factories = new Dictionary<Type, Func<object>>();

        public static void Register(Type t, Func<object> factory)
        {
            if(factories.ContainsKey(t))
            {
                factories[t] = factory;
            }
            else
            {
                factories.Add(t, factory);
            }
        }

        public static void UnRegister(Type t)
        {
            factories.Remove(t);
        }

        public static object CreateInstance(Type t)
        {
            if (factories.ContainsKey(t))
            {
                return factories[t]();
            }
            else
            {
                return Activator.CreateInstance(t);
            }
        }

        public static T CreateInstance<T>(string name)
        {
            var candidate = Assembly.GetExecutingAssembly().ExportedTypes.Where(t => t.Name.ToLower() == name.ToLower()).FirstOrDefault();

            if (candidate == null)
            {
                candidate = Assembly.GetEntryAssembly().ExportedTypes.Where(t => t.Name.ToLower() == name.ToLower()).FirstOrDefault();
            }

            if (candidate == null)
            {
                throw new ArgumentException("Could not resolve type: " + name);
            }

            return (T)CreateInstance(candidate);
        }
    }
}
