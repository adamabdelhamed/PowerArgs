using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace PowerArgs
{
    internal static class ObjectEx
    {
        private sealed class MainMethod
        {
            internal MethodInfo Method { get; }
            internal MainMethod(MethodInfo method) => Method = method;
        }

        private static readonly ConcurrentDictionary<Type, MainMethod> MainMethods = new ConcurrentDictionary<Type, MainMethod>();

        internal static MethodInfo InvokeMainMethod(this object o)
        {
            var method = MainMethods.GetOrAdd(o.GetType(), FindMainMethod).Method;

            var ret = InvocationDelegateCache.Invoke(method, o, Array.Empty<object>());
            if (ret is Task task) task.GetAwaiter().GetResult();
            return method;
        }

        private static MainMethod FindMainMethod(Type type)
        {
            var method = type.GetMethod("Main", BindingFlags.Instance | BindingFlags.Public);
            if (method == null) throw new InvalidArgDefinitionException("There is no Main() method in type " + type.Name);
            if (method.IsStatic) throw new InvalidArgDefinitionException("The Main() method in type '" + type.Name + "' must not be static");
            if (method.GetParameters().Length > 0) throw new InvalidArgDefinitionException("The Main() method in type '" + type.Name + "' must not take any parameters");
            if (method.ReturnType != null && method.ReturnType != typeof(void) && method.ReturnType != typeof(Task)) throw new InvalidArgDefinitionException("The Main() method in type '" + type.Name + "' must return void or Task");
            return new MainMethod(method);
        }
    }
}
