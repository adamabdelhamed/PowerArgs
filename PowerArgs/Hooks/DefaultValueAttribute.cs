using System;

namespace PowerArgs
{
    /// <summary>
    /// Use this attribute to set the default value for a parameter.  Note that this only
    /// works for simple types since only compile time constants can be passed to an attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class DefaultValueAttribute : ArgHook, ICommandLineArgumentMetadata
    {
        /// <summary>
        /// The default value that was specified on the attribute.  Note that the value will get
        /// converted to a string and then fed into the parser to be revived.
        /// </summary>
        public object Value { get; protected set; }

        /// <summary>
        /// Creates a new DefaultValueAttribute with the given value.  Note that the value will get
        /// converted to a string and then fed into the parser to be revived.
        /// </summary>
        /// <param name="value">The default value for the property</param>
        public DefaultValueAttribute(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new DefaultValueAttribute without value. Value can be specified in derived class.
        /// Note that the value will get converted to a string and then fed into the parser to be revived.
        /// </summary>
        protected DefaultValueAttribute()
        {
        }

        /// <summary>
        /// Before the property is revived and validated, if the user didn't specify a value, 
        /// then substitue the default value.
        /// 
        /// </summary>
        /// <param name="Context"></param>
        public override void BeforePopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue == null) Context.ArgumentValue = Value.ToString();
        }
    }

    /// <summary>
    /// Use this attribute to set the default value for a parameter.  Note that this only
    /// works for simple types since only compile time constants can be passed to an attribute.
    /// </summary>
    public class ArgDefaultValueAttribute : DefaultValueAttribute 
    {
        /// <summary>
        /// Creates a new ArgDefaultValueAttribute with the given value.  Note that the value will get
        /// converted to a string and then fed into the parser to be revived.
        /// </summary>
        /// <param name="value">The default value for the property</param>
        public ArgDefaultValueAttribute(object value) : base(value) { }
    }
}
