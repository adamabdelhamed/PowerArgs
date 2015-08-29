using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace PowerArgs
{
    internal static class ObjectEx
    {
        internal static MethodInfo InvokeMainMethod(this object o)
        {
            var method = o.GetType().GetMethod("Main");
            if (method == null) throw new InvalidArgDefinitionException("There is no Main() method in type " + o.GetType().Name);
            if (method.IsStatic) throw new InvalidArgDefinitionException("The Main() method in type '" + o.GetType().Name + "' must not be static");
            if (method.GetParameters().Length > 0) throw new InvalidArgDefinitionException("The Main() method in type '" + o.GetType().Name + "' must not take any parameters");
            if (method.ReturnType != null && method.ReturnType != typeof(void) && method.ReturnType != typeof(Task)) throw new InvalidArgDefinitionException("The Main() method in type '" + o.GetType().Name + "' must return void or Task");

            try
            {
                var ret = method.Invoke(o, new object[0]);
                
                if(ret is Task)
                {
                    (ret as Task).Wait();
                }
            }
            catch(Exception ex)
            {
                if (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                else
                {
                    throw;
                }
            }
            return method;
        }
    }
}
