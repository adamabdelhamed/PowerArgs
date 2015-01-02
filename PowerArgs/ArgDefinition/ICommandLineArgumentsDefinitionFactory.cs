using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// An interface that defines a contract for creating command line argument definitions
    /// </summary>
    public interface ICommandLineArgumentsDefinitionFactory
    {
        /// <summary>
        /// Creates a definition given a base definition
        /// </summary>
        /// <param name="other">the base definition</param>
        /// <returns>the definition instance</returns>
        CommandLineArgumentsDefinition MakeDefinition(CommandLineArgumentsDefinition other);
        /// <summary>
        /// Creates a definition instance
        /// </summary>
        /// <returns>the definition instance</returns>
        CommandLineArgumentsDefinition MakeDefinition();
    }

    internal class ArgumentScaffoldTypeCommandLineDefinitionFactory : ICommandLineArgumentsDefinitionFactory
    {
        public CommandLineArgumentsDefinition MakeDefinition(CommandLineArgumentsDefinition other)
        {
            if (other.ArgumentScaffoldType == null) throw new NotSupportedException("Your command line arguments definition was not created from a scaffold type. You created it manually using the default constructor of CommandLineArgumentsDefinition().  If you want to use ArgPipeline you must implement ICommandLineDefinitionFactory and pass your custom type to the ArgPipelineAttribute's CommandLineDefinitionFactory property");
            return new CommandLineArgumentsDefinition(other.ArgumentScaffoldType);
        }


        public CommandLineArgumentsDefinition MakeDefinition()
        {
            throw new NotImplementedException("This class only implements the overload of MakeDefinition that takes in a definition as a parameter");
        }
    }

    /// <summary>
    /// A helper class that lets you create definition factories from Funcs
    /// </summary>
    public class CommandLineArgumentsDefinitionFactory : ICommandLineArgumentsDefinitionFactory
    {
        Func<CommandLineArgumentsDefinition, CommandLineArgumentsDefinition> fromOtherImpl;
        Func<CommandLineArgumentsDefinition> fromNothingImpl;
        private CommandLineArgumentsDefinitionFactory(Func<CommandLineArgumentsDefinition, CommandLineArgumentsDefinition> fromOtherImpl, Func<CommandLineArgumentsDefinition> fromNothingImpl) 
        {
            this.fromOtherImpl = fromOtherImpl ?? ((other) => { throw new NotImplementedException(); });
            this.fromNothingImpl = fromNothingImpl ?? (()=> {throw new NotImplementedException();});
        }

        /// <summary>
        /// Creates a factory that can create a definition from a base definition
        /// </summary>
        /// <param name="fromOtherImpl">An implementation that can create one definition from another</param>
        /// <returns>the factory</returns>
        public static ICommandLineArgumentsDefinitionFactory Create(Func<CommandLineArgumentsDefinition, CommandLineArgumentsDefinition> fromOtherImpl)
        {
            return new CommandLineArgumentsDefinitionFactory(fromOtherImpl, null);
        }

        /// <summary>
        /// Creates a factory that can create a definition
        /// </summary>
        /// <param name="fromNothingImpl">An implementation that can create a defunutuin</param>
        /// <returns>the factory</returns>
        public static ICommandLineArgumentsDefinitionFactory Create(Func<CommandLineArgumentsDefinition> fromNothingImpl)
        {
            return new CommandLineArgumentsDefinitionFactory(null, fromNothingImpl);
        }

        /// <summary>
        /// Creates a factory that can create a definition from a base definition or from nothing
        /// </summary>
        /// <param name="fromOtherImpl">An implementation that can create one definition from another</param>
        /// <param name="fromNothingImpl">An implementation that can create a defunutuin</param>
        /// <returns>the factory</returns>
        public static ICommandLineArgumentsDefinitionFactory Create(Func<CommandLineArgumentsDefinition, CommandLineArgumentsDefinition> fromOtherImpl, Func<CommandLineArgumentsDefinition> fromNothingImpl)
        {
            return new CommandLineArgumentsDefinitionFactory(fromOtherImpl, fromNothingImpl);
        }

        /// <summary>
        /// Creates a definition from a base using the provided Func
        /// </summary>
        /// <param name="other">the base definition</param>
        /// <returns>a definition</returns>
        public CommandLineArgumentsDefinition MakeDefinition(CommandLineArgumentsDefinition other)
        {
            return fromOtherImpl(other);
        }

        /// <summary>
        /// Creates a definition using the provided Func
        /// </summary>
        /// <returns>a definition</returns>
        public CommandLineArgumentsDefinition MakeDefinition()
        {
            return fromNothingImpl();
        }
    }
}
