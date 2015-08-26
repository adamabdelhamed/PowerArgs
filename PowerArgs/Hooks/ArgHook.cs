using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace PowerArgs
{
    /// <summary>
    /// Creates a hook from an action
    /// </summary>
    internal class SingleActionHook : ArgHook
    {
        // TODO - write test to make sure I don't ever miss a hook

        /// <summary>
        /// Gets the hook implementation that was passed to the constructor
        /// </summary>
        public Action<HookContext> HookImpl { get; private set; }

        /// <summary>
        /// Gets the Id or name of the hook
        /// </summary>
        public string HookId { get; set; }

        /// <summary>
        /// Creates a new hook with the given name, priority, and implementation
        /// </summary>
        /// <param name="hookId">The id or name of the hook</param>
        /// <param name="priority">The priority of the hook (higher numbers execute first)</param>
        /// <param name="hookImpl">The hook implementation</param>
        public SingleActionHook(string hookId, int priority, Action<HookContext> hookImpl)
        {
            this.HookId = hookId;
            this.HookImpl = hookImpl;

            var priorityProperty = GetType().GetProperty(HookId + "Priority");
            if(priorityProperty == null)
            {
                throw new InvalidArgDefinitionException("Unknown hook id: " + HookId);
            }

            priorityProperty.SetValue(this, priority, null);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void AfterCancel(HookContext context)
        {
            DoHook("AfterCancel", context);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void AfterInvoke(HookContext context)
        {
            DoHook("AfterInvoke", context);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void AfterPopulateProperties(HookContext context)
        {
            DoHook("AfterPopulateProperties", context);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void AfterPopulateProperty(HookContext context)
        {
            DoHook("AfterPopulateProperty", context);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void BeforeInvoke(HookContext context)
        {
            DoHook("BeforeInvoke", context);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void BeforeParse(HookContext context)
        {
            DoHook("BeforeParse", context);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void BeforePopulateProperties(HookContext context)
        {
            DoHook("BeforePopulateProperties", context);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void BeforePopulateProperty(HookContext context)
        {
            DoHook("BeforePopulateProperty", context);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void BeforePrepareUsage(HookContext context)
        {
            DoHook("BeforePrepareUsage", context);
        }

        /// <summary>
        /// Calls the underlying hook if it was specified in the constructor
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void BeforeValidateDefinition(HookContext context)
        {
            DoHook("BeforeValidateDefinition", context);
        }

        private void DoHook(string runningHookId, HookContext context)
        {
            if (runningHookId == HookId)
            {
                try
                {
                    HookImpl(context);
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
            }
        }
    }

    /// <summary>
    /// An abstract class that you can implement if you want to hook into various parts of the
    /// parsing pipeline.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Parameter)]
    public abstract class ArgHook : Attribute, IGlobalArgMetadata
    {
        /// <summary>
        /// Context that is passed to your hook.  Different parts of the context will be available
        /// depending on which part of the pipeline you're hooking into.
        /// </summary>
        public class HookContext
        {
            [ThreadStatic]
            private static HookContext _current;

            /// <summary>
            /// Gets the context for the current parse operation happening on the current thread or
            /// null if no parse is happening on the current thread.
            /// </summary>
            public static HookContext Current
            {
                get
                {
                    return _current;
                }
                internal set
                {
                    _current = value;
                }
            }

            /// <summary>
            /// The current property being operating on.  This is not available during BeforePopulateProperties or
            /// AfterPopulateProperties.
            /// </summary>
            [Obsolete("You should use CurrentArgument instead of Property since it offers more metadata.  It also exposes the PropertyInfo via CommandLineArgument.Source if the argument was created from a PropertyInfo.")]
            public PropertyInfo Property { get; set; }

            /// <summary>
            /// The current argument being operating on. 
            /// AfterPopulateProperties.
            /// </summary>
            public CommandLineArgument CurrentArgument { get; set; }

            /// <summary>
            /// Gets the action that was specified on the command line or null if no action was specified or if the definition exposes no actions.
            /// </summary>
            public CommandLineAction SpecifiedAction
            {
                get
                {
                    return Definition.SpecifiedAction;
                }
                internal set
                {
                    Definition.SpecifiedAction = value;
                }
            }

            /// <summary>
            /// The command line arguments that were passed to the Args class.  This is always available and you
            /// can modify it in the BeforeParse hook at your own risk.
            /// </summary>
            public string[] CmdLineArgs;

            /// <summary>
            /// The string value that was specified for the current argument.  This will align with the value of ArgHook.CurrentArgument.
            /// 
            /// This is not available during BeforePopulateProperties or
            /// AfterPopulateProperties.
            /// 
            /// </summary>
            public string ArgumentValue;

            /// <summary>
            /// This is the instance of your argument class.  The amount that it is populated will depend on how far along in the pipeline
            /// the parser is.
            /// </summary>
            public object Args { get; set; }

            /// <summary>
            /// The definition that's being used throughout the parsing process
            /// </summary>
            public CommandLineArgumentsDefinition Definition { get; set; }

            /// <summary>
            /// This is the value of the current property after it has been converted into its proper .NET type.  It is only available
            /// to the AfterPopulateProperty hook.
            /// </summary>
            public object RevivedProperty;

            /// <summary>
            /// The raw parser data.  This is not available to the BeforeParse hook.  It may be useful for you to look at, but you should rarely have to change the values.  
            /// Modify the content of this at your own risk.
            /// </summary>
            public ParseResult ParserData { get; set; }

            /// <summary>
            /// Get a value from the context's property bag.
            /// </summary>
            /// <typeparam name="T">The type of value you are expecting</typeparam>
            /// <param name="key">The key for the property you want to get</param>
            /// <returns>The value or default(T) if no value was found.</returns>
            public T GetProperty<T>(string key)
            {
                var val = this[key];
                if (val == null)
                {
                    if (typeof(T).IsClass) return default(T);
                    else throw new KeyNotFoundException("There is no property named '" + key + "'");
                }
                else return (T)val;
            }

            /// <summary>
            /// Set a value in the context's property bag.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="key">The key for the property you want to set</param>
            /// <param name="value">The value of the property to set</param>
            public void SetProperty<T>(string key, T value)
            {
                this[key] = value;
            }

            /// <summary>
            /// Returns true if the context has a value for the given property.
            /// </summary>
            /// <param name="key">The key to check</param>
            /// <returns>true if the context has a value for the given property, false otherwise</returns>
            public bool HasProperty(string key)
            {
                return _properties.ContainsKey(key);
            }

            /// <summary>
            /// Clear a value in the context's property bag.
            /// </summary>
            /// <param name="key">The key for the property you want to clear.</param>
            public void ClearProperty(string key)
            {
                this[key] = null;
            }

            /// <summary>
            /// Stops all argument processing, hooks, and action invocation as soon as is feasable.  You
            /// can implement an ArgHook that receives an event when this is called.
            /// </summary>
            public void CancelAllProcessing()
            {
                this.RunAfterCancel();
                throw new ArgCancelProcessingException();
            }

            private Dictionary<string, object> _properties = new Dictionary<string, object>();
            private object this[string key]
            {
                get
                {
                    object ret;
                    if (_properties.TryGetValue(key, out ret))
                    {
                        return ret;
                    }
                    return null;
                }
                set
                {
                    if (_properties.ContainsKey(key))
                    {
                        if (value != null)
                        {
                            _properties[key] = value;
                        }
                        else
                        {
                            _properties.Remove(key);
                        }
                    }
                    else
                    {
                        if (value != null)
                        {
                            _properties.Add(key, value);
                        }
                    }
                }
            }

            private class ContextualHookInfo
            {
                public ArgHook Hook { get; set; }
                public CommandLineArgument Argument { get; set; }
                public PropertyInfo Property { get; set; }
            }

            internal void RunHook(Func<ArgHook, int> orderby, Action<ArgHook> hookAction)
            {
                List<ContextualHookInfo> hooksToRun = new List<ContextualHookInfo>();

                var seen = new List<PropertyInfo>();

                hooksToRun.AddRange(Definition.Hooks.Select(h => new ContextualHookInfo { Hook = h }));
                
                foreach (var argument in Definition.Arguments)
                {
                    if (argument.Source as PropertyInfo != null) seen.Add(argument.Source as PropertyInfo);

                    hooksToRun.AddRange(argument.Hooks.Select(h => new ContextualHookInfo 
                    { 
                        Hook = h, 
                        Argument = argument,
                        Property = argument.Source as PropertyInfo 
                    }));
                }

                if (Definition.ArgumentScaffoldType != null)
                {
                    foreach (var property in Definition.ArgumentScaffoldType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => seen.Contains(p) == false))
                    {
                        hooksToRun.AddRange(property.Attrs<ArgHook>().Select(h => new ContextualHookInfo
                        {
                            Hook = h,
                            Property = property
                        }));
                    }
                }

                foreach (var action in Definition.Actions)
                {
                    if (Definition.SpecifiedAction == null || action == Definition.SpecifiedAction)
                    {
                        foreach (var argument in action.Arguments)
                        {
                            hooksToRun.AddRange(argument.Hooks.Select(h => new ContextualHookInfo
                            {
                                Hook = h,
                                Argument = argument,
                                Property = argument.Source as PropertyInfo
                            }));
                        }
                    }
                }

                hooksToRun = hooksToRun.OrderByDescending(info => orderby(info.Hook)).ToList();

                foreach(var info in hooksToRun)
                {
                    this.CurrentArgument = info.Argument;
                    this.Property = info.Property;
                    hookAction(info.Hook);
                    this.CurrentArgument = null;
                    this.Property = null;
                }
            }

            internal void RunBeforeParse()
            {
                RunHook(h => h.BeforeParsePriority, (h) => { h.BeforeParse(this); });
            }

            internal void RunBeforePopulateProperties()
            {
                RunHook(h => h.BeforePopulatePropertiesPriority, (h) => { h.BeforePopulateProperties(this); });
            }

            internal void RunAfterPopulateProperties()
            {
                RunHook(h => h.AfterPopulatePropertiesPriority, (h) => { h.AfterPopulateProperties(this); });
            }

            internal void RunBeforeInvoke()
            {
                RunHook(h => h.BeforeInvokePriority, (h) => { h.BeforeInvoke(this); });
            }

            internal void RunAfterInvoke()
            {
                RunHook(h => h.AfterInvokePriority, (h) => { h.AfterInvoke(this); });
            }

            internal void RunAfterCancel()
            {
                RunHook(h => h.AfterCancelPriority, (h) => { h.AfterCancel(this); });
            }

            internal void RunBeforePrepareUsage()
            {
                RunHook(h => h.BeforePrepareUsagePriority, (h) => { h.BeforePrepareUsage(this); });
            }

            internal void RunBeforeValidateDefinition()
            {
                RunHook(h => h.BeforeValidateDefinitionPriority, (h) => { h.BeforeValidateDefinition(this); });
            }
        }

        
        /// <summary>
        /// The priority of the BeforeValidateDefinition hook.  Higher numbers execute first.
        /// </summary>
        public int BeforeValidateDefinitionPriority { get; set; }

        /// <summary>
        /// The priority of the BeforePrepareUsage hook.  Higher numbers execute first.
        /// </summary>
        public int BeforePrepareUsagePriority { get; set; }

        /// <summary>
        /// The priority of the BeforeParse hook.  Higher numbers execute first.
        /// </summary>
        public int BeforeParsePriority { get; set; }

        /// <summary>
        /// The priority of the BeforePopulateProperties hook.  Higher numbers execute first.
        /// </summary>
        public int BeforePopulatePropertiesPriority { get; set; }

        /// <summary>
        /// The priority of the BeforePopulateProperty hook.  Higher numbers execute first.
        /// </summary>
        public int BeforePopulatePropertyPriority { get; set; }

        /// <summary>
        /// The priority of the AfterPopulateProperty hook.  Higher numbers execute first.
        /// </summary>
        public int AfterPopulatePropertyPriority { get; set; }

        /// <summary>
        /// The priority of the AfterPopulateProperties hook.  Higher numbers execute first.
        /// </summary>
        public int AfterPopulatePropertiesPriority { get; set; }

        /// <summary>
        /// The priority of the BeforeInvoke hook.  Higher numbers execute first.
        /// </summary>
        public int BeforeInvokePriority { get; set; }

        /// <summary>
        /// The priority of the AfterInvoke hook.  Higher numbers execute first.
        /// </summary>
        public int AfterInvokePriority { get; set; }

        /// <summary>
        /// The priority of the AfterCancel hook.  Higher numbers execute first.
        /// </summary>
        public int AfterCancelPriority { get; set; }

        /// <summary>
        /// This hook is called before the definition is validated for structural issues
        /// </summary>
        /// <param name="context"></param>
        public virtual void BeforeValidateDefinition(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateBeforeValidateDefinitionHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("BeforeValidateDefinition", priority, action);
        }

        /// <summary>
        /// This hook is called before the template based usage system prepares the usage documentation
        /// </summary>
        /// <param name="context"></param>
        public virtual void BeforePrepareUsage(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateBeforePrepareUsageHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("BeforePrepareUsage", priority, action);
        }

        /// <summary>
        /// This hook is called before the parser ever looks at the command line.  You can do some preprocessing of the 
        /// raw string arguments here.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void BeforeParse(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateBeforeParseHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("BeforeParse", priority, action);
        }

        /// <summary>
        /// This hook is called before the arguments defined in a class are populated.  For actions (or sub commands) this hook will
        /// get called once for the main class and once for the specified action.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void BeforePopulateProperties(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateBeforePopulatePropertiesHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("BeforePopulateProperties", priority, action);
        }

        /// <summary>
        /// This hook is called before an argument is transformed from a string into a proper type and validated.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void BeforePopulateProperty(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateBeforePopulatePropertyHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("BeforePopulateProperty", priority, action);
        }

        /// <summary>
        /// This hook is called after an argument is transformed from a string into a proper type and validated.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void AfterPopulateProperty(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateAfterPopulatePropertyHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("AfterPopulateProperty", priority, action);
        }

        /// <summary>
        /// This hook is called after the arguments defined in a class are populated.  For actions (or sub commands) this hook will
        /// get called once for the main class and once for the specified action.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void AfterPopulateProperties(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateAfterPopulatePropertiesHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("AfterPopulateProperties", priority, action);
        }

        /// <summary>
        /// This hook is called after parsing is complete, but before any Action or Main method is invoked.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void BeforeInvoke(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateBeforeInvokeHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("BeforeInvoke", priority, action);
        }

        /// <summary>
        /// This hook is called after any Action or Main method is invoked.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void AfterInvoke(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateAfterInvokeHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("AfterInvoke", priority, action);
        }

        /// <summary>
        /// This hook is called if CancelAllProcessing() is called on a HookContext object.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void AfterCancel(HookContext context) { }

        /// <summary>
        /// Creates a hook that targets the corresponding hook method given an implementation and a priority
        /// </summary>
        /// <param name="action">The hook implementation</param>
        /// <param name="priority">The hook priority (higher executes first)</param>
        /// <returns>The hook</returns>
        public static ArgHook CreateAfterCancelHook(Action<HookContext> action, int priority = 0)
        {
            return new SingleActionHook("AfterCancel", priority, action);
        }
    }
}
