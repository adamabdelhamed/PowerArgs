using System;

namespace PowerArgs
{
    /// <summary>
    /// Use this attribute if your action implementation methods are defined in a type other than the 
    /// type being passed to Args.ParseAction() or Args.InvokeAction().  You can add multiple attributes
    /// of this type if you want to combine actions from multiple classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ArgActionType : Attribute, ICommandLineArgumentsDefinitionMetadata
    {
        /// <summary>
        /// The type that implements your action methods.
        /// </summary>
        public Type ActionType { get; private set; }

        /// <summary>
        /// Creates a new ArgActionType attribute given the type that contains the action implementation.
        /// </summary>
        /// <param name="t">The type that implements your action methods.</param>
        public ArgActionType(Type t)
        {
            this.ActionType = t;
        }
    }
}
