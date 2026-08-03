using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace PowerArgs
{
    internal static class InvocationDelegateCache
    {
        private static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> Invokers = new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();

        internal static object Invoke(MethodInfo method, object instance, object[] arguments) => Invokers.GetOrAdd(method, CreateInvoker)(instance, arguments);

        private static Func<object, object[], object> CreateInvoker(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var arguments = Expression.Parameter(typeof(object[]), "arguments");
            var parameters = method.GetParameters();
            var convertedArguments = new Expression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++) convertedArguments[i] = Expression.Convert(Expression.ArrayIndex(arguments, Expression.Constant(i)), parameters[i].ParameterType);
            var call = Expression.Call(method.IsStatic ? null : Expression.Convert(instance, method.DeclaringType), method, convertedArguments);
            Expression body = method.ReturnType == typeof(void) ? Expression.Block(call, Expression.Constant(null, typeof(object))) : Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object, object[], object>>(body, instance, arguments).Compile();
        }
    }
}
