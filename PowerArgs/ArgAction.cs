using System.Reflection;
using System;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// This is the more complex version of the public result that is produced by the parser.
    /// </summary>
    /// <typeparam name="T">Represents the custom argument scaffold type that was passed to the parser.</typeparam>
    public class ArgAction<T> : ArgAction
    {
        /// <summary>
        /// The instance of your custom scaffold type that the parser generated and parsed.
        /// </summary>
        public T Args
        {
            get { return (T)Value; }
            set { Value = value; }
        }

        /// <summary>
        /// This will find the implementation method for your action and invoke it, passing the action specific
        /// arguments as a parameter.
        /// </summary>
        public void Invoke()
        {
            if (Args == null || (ActionArgs == null && !this.EmptyArgActionValue)) throw new MissingArgException("No action was specified");
            var resolvedMethod = ResolveMethod(ActionArgsProperty);
            
            // if possible call the action directly, rather than using a reflection invocation
            if (resolvedMethod.Action == null)
            {
                resolvedMethod.MethodInfo.Invoke(null, new object[] { ActionArgs });
            }
            else
            {
                resolvedMethod.Action((dynamic)ActionArgs);
            }
        }

        /// <summary>
        /// Given an action property, finds the method that implements the action.
        /// </summary>
        /// <param name="actionProperty">The property to resolve</param>
        /// <returns></returns>
        public static ResolveMethodResults ResolveMethod(PropertyInfo actionProperty)
        {
            return ArgAction.ResolveMethod(typeof(T), actionProperty);
        }
    }

    /// <summary>
    /// This is the weakly typed, more complex version of the public result that is produced by the parser.
    /// </summary>
    public class ArgAction
    {
        /// <summary>
        /// The results from resolving the action method to execute.
        /// </summary>
        public class ResolveMethodResults
        {
            /// <summary>
            /// The method info of the action to execute. Used for reflection invocation.
            /// </summary>
            public MethodInfo MethodInfo { get; set; }

            /// <summary>
            /// The System.Action&lt;TActionArgs&gt; to execute. Will be null unless
            /// an appropriate method is found. Note: This field is dynamic.
            /// </summary>
            public dynamic Action { get;  internal set; }

            /// <summary>
            /// The expected return type of the action.
            /// </summary>
            public Type ExpectedReturnType { get; set; }
        }

        /// <summary>
        /// The instance of your custom scaffold type that the parser generated and parsed.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// If you used the action framework then this will represent the instance of the action specific arguments
        /// that were parsed.
        /// </summary>
        public object ActionArgs { get; set; }

        /// <summary>
        /// If you used the action framework then this will map to the property that the user specified as the first
        /// parameter on the command line.
        /// </summary>
        public PropertyInfo ActionArgsProperty { get; set; }

        public bool EmptyArgActionValue { get; internal set; }

        internal static PropertyInfo GetActionProperty<T>()
        {
            return GetActionProperty(typeof(T));
        }

        internal static PropertyInfo GetActionProperty(Type t)
        {
            var actionProperty = (from p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                  where p.Name == Constants.ActionPropertyConventionName &&
                                        p.Attr<ArgPosition>() != null && p.Attr<ArgPosition>().Position == 0 &&
                                        p.HasAttr<ArgRequired>()
                                  select p).SingleOrDefault();
            return actionProperty;
        }

        internal static ResolveMethodResults ResolveMethod(Type t, PropertyInfo actionProperty)
        {
            string methodName = actionProperty.Name;
            int end = methodName.LastIndexOf(Constants.ActionArgConventionSuffix);
            if (end < 1)
            {
                throw new InvalidArgDefinitionException(
                    "Could not resolve action method from property name: " + actionProperty.Name);
            }
            methodName = methodName.Substring(0, end);

            var actionType = t.HasAttr<ArgActionType>() ? t.Attr<ArgActionType>().ActionType : t;

            var method = actionType.GetMethod(methodName);
            if (method == null) throw new InvalidArgDefinitionException("Could not find action method '" + methodName + "'");
            
            var arity = method.GetParameters().Length;

            // two options here:
            Type expectedReturnType;
            dynamic action = null;
            if (arity == 1)
            {
                // either exisiting convention (public static void Action(T args))
                expectedReturnType = method.ReturnType;
            }
            else if (arity == 0)
            {
                // or an alternate convention (public static System.Action<T> Action())

                expectedReturnType = typeof(Action<>).MakeGenericType(actionProperty.PropertyType);

                if (method.IsStatic == false) throw new InvalidArgDefinitionException("PowerArg action methods must be static");
                if (method.ReturnType == typeof(void)) throw new InvalidArgDefinitionException(string.Format("PowerArgs action methods must not return void - it should return {0}", expectedReturnType));
                if (method.ReturnType != expectedReturnType)
                    throw new InvalidArgDefinitionException(
                        string.Format("PowerArgs action methods return type {0} does not match expected return type {1}", method.ReturnType, expectedReturnType));

                // since it is the alternate convention, execute the method, to return the actual action we want to execute.
                // override existing method
                action = method.Invoke(null, new object[] {});
                method = action.Method;
            }
            else
            {
                throw new InvalidArgDefinitionException("PowerArg action methods must take one parameter that matches the property type for the attribute");
            }

            if (method.IsStatic == false) throw new InvalidArgDefinitionException("PowerArg action methods must be static");
            if (method.GetParameters().Length != 1) throw new InvalidArgDefinitionException("PowerArg action methods must take one parameter that matches the property type for the attribute");
            if (method.GetParameters()[0].ParameterType != actionProperty.PropertyType) throw new InvalidArgDefinitionException(string.Format("Argument of type {0} does not match expected type {1}", actionProperty.PropertyType, method.GetParameters()[0].ParameterType));

            return new ResolveMethodResults() {MethodInfo = method, Action = action, ExpectedReturnType = expectedReturnType};
        }
    }
}
