using System.Reflection;
using System;
using System.Linq;

namespace PowerArgs
{
    public class ArgAction<T>
    {
        public T Args { get; set; }
        public object ActionArgs { get; set; }
        public PropertyInfo ActionArgsProperty { get; set; }

        public void Invoke()
        {
            if (Args == null || ActionArgs == null) throw new ArgException("No action was specified");
            ResolveMethod(ActionArgsProperty).Invoke(null, new object[] { ActionArgs });
        }

        public static MethodInfo ResolveMethod(PropertyInfo actionProperty)
        {
            return ArgAction.ResolveMethod(typeof(T), actionProperty);
        }
    }

    public class ArgAction
    {
        public static PropertyInfo GetActionProperty<T>()
        {
            return GetActionProperty(typeof(T));
        }

        public static PropertyInfo GetActionProperty(Type t)
        {
            var actionProperty = (from p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                  where p.Name == ArgSettings.ActionPropertyConventionName &&
                                        p.Attr<ArgPosition>() != null && p.Attr<ArgPosition>().Position == 0 &&
                                        p.HasAttr<ArgRequired>()
                                  select p).SingleOrDefault();
            return actionProperty;
        }

        public static MethodInfo ResolveMethod(Type t, PropertyInfo actionProperty)
        {
            string methodName = actionProperty.Name;
            int end = methodName.LastIndexOf(ArgSettings.ActionArgConventionSuffix);
            if (end < 1) throw new InvalidArgDefinitionException("Could not resolve action method from property name: " + actionProperty.Name);
            methodName = methodName.Substring(0, end);

            var actionType = t.HasAttr<ArgActionType>() ? t.Attr<ArgActionType>().ActionType : t;
            var method = actionType.GetMethod(methodName);
            if (method == null) throw new InvalidArgDefinitionException("Could not find action method '" + methodName + "'");

            if (method.IsStatic == false) throw new InvalidArgDefinitionException("PowerArg action methods must be static");
            if (method.GetParameters().Length != 1) throw new InvalidArgDefinitionException("PowerArg action methods must take one parameter that matches the property type for the attribute");
            if (method.GetParameters()[0].ParameterType != actionProperty.PropertyType) throw new InvalidArgDefinitionException(string.Format("Argument of type {0} does not match expected type {1}", actionProperty.PropertyType, method.GetParameters()[0].ParameterType));

            return method;
        }
    }
}
