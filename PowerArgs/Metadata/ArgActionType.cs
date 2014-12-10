using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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

    /// <summary>
    /// An attribute you can put on classes that contain arg action methods that can be imported into a program
    /// that uses an [ArgActionResolver] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ArgActions : Attribute
    {

    }

    /// <summary>
    /// An attribute you can put on a program's main scaffold class in order to import external types that contain
    /// action methods.  By default, this resolver will search the program's entry assembly for types that contain the
    /// [ArgActions] attribute.  You can derive from this class and override the ResolveActionTypes method if you want to
    /// perform custom resolution of actions.
    /// </summary>
    public class ArgActionResolver : Attribute, ICommandLineArgumentsDefinitionMetadata
    {
        /// <summary>
        /// Searches the program's entry assembly for types that contain an [ArgActions] attribute and returns those
        /// types so that PowerArgs can import them into the running program.  This method is marked as virtual so that
        /// classes that derive from ArgActionResolver can implement their own resolution strategy.
        /// </summary>
        /// <returns>matching types</returns>
        public virtual IEnumerable<Type> ResolveActionTypes()
        {
            return Assembly.GetEntryAssembly().GetTypes().Where(t => t.HasAttr<ArgActions>());
        }
    }
}
