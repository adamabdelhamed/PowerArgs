using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Linq;

namespace PowerArgs
{
    internal static class ReflectionMemberCache
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PublicInstanceProperties = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PublicProperties = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<Type, MethodInfo[]> PublicMethods = new ConcurrentDictionary<Type, MethodInfo[]>();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ArgumentProperties = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ActionProperties = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<Type, MethodInfo[]> ActionMethods = new ConcurrentDictionary<Type, MethodInfo[]>();

        internal static PropertyInfo[] GetPublicInstanceProperties(Type type) => PublicInstanceProperties.GetOrAdd(type, value => value.GetProperties(BindingFlags.Instance | BindingFlags.Public));
        internal static PropertyInfo[] GetPublicProperties(Type type) => PublicProperties.GetOrAdd(type, value => value.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public));
        internal static MethodInfo[] GetPublicMethods(Type type) => PublicMethods.GetOrAdd(type, value => value.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public));
        internal static PropertyInfo[] GetArgumentProperties(Type type) => ArgumentProperties.GetOrAdd(type, value => GetPublicInstanceProperties(value).Where(CommandLineArgument.IsArgument).ToArray());
        internal static PropertyInfo[] GetActionProperties(Type type) => ActionProperties.GetOrAdd(type, value => GetPublicProperties(value).Where(CommandLineAction.IsActionImplementation).ToArray());
        internal static MethodInfo[] GetActionMethods(Type type) => ActionMethods.GetOrAdd(type, value => GetPublicMethods(value).Where(CommandLineAction.IsActionImplementation).ToArray());
    }
}
