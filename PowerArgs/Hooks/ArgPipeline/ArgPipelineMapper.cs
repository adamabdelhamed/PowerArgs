using System;
using System.Linq;

namespace PowerArgs.Preview
{
    /// <summary>
    /// An interface that defines how objects are mapped when being passed along a pipeline
    /// </summary>
    public interface IArgPipelineObjectMapper
    {
        /// <summary>
        /// Convert the given incompatible object to the desired type.  This is used when a pipeline stage receives an object that is of a different type
        /// than it expects.  PowerArgs has a built in algorithm for doing this type of mapping, but you may find the need to customize it.
        /// </summary>
        /// <param name="desiredType">The type of object that the caller would like to be returned</param>
        /// <param name="incompatibleObject">The object that needs to be converted</param>
        /// <returns>The converted, compatible object</returns>
        object MapIncompatibleDirectTargets(Type desiredType, object incompatibleObject);

        /// <summary>
        /// Implementers should try to look at the given object and extract a string key and value that corresponds to the given CommandLineArgument. This is used
        /// when a pipeline stage receives an object and that stage does not define an explicit pipeline target.  In this case we need to find a way to individually map the object
        /// to loose command line arguments in the target stage.
        /// </summary>
        /// <param name="o">The pipeline object</param>
        /// <param name="argument">The argument we're attempting to extract</param>
        /// <param name="staticMappings">A static set of mappings that you should honor when trying to map properties on the pipeline object.  You may be passed a null for this</param>
        /// <param name="commandLineKey">The extracted command line key that should be populated if you return true</param>
        /// <param name="commandLineValue">The extracted command line value that should be populated if you return true</param>
        /// <returns>True if an argument was successfully extracted, false otherwise</returns>
        bool TryExtractObjectPropertyIntoCommandLineArgument(object o, CommandLineArgument argument, string[] staticMappings, out string commandLineKey, out string commandLineValue);
    }

    /// <summary>
    /// Provides a way to get to the current mapper
    /// </summary>
    public static class ArgPipelineObjectMapper
    {
        /// <summary>
        /// Gets or sets the current pipeline object mapper.  By default, PowerArgs does not set this.
        /// </summary>
        public static IArgPipelineObjectMapper CurrentMapper { get; set; }
    }

    /// <summary>
    /// An attribute that lets you declare that a particular argument can be populated from a pipeline object's property whose name is not
    /// one of the argument's supported command line aliases. For example if you had a command line argument with default alias 'CustomerId' and an
    /// object came into the pipeline with a property called 'Id' then you might want to allow that mapping.  In this case you would add the following attribute to your argument:
    /// 
    /// [ArgPipelineExtractor("Id")]
    /// public string CustomerId { get; set; }
    /// 
    /// </summary>
    public class ArgPipelineExtractor : Attribute, ICommandLineArgumentMetadata
    {
        /// <summary>
        /// The static mappings that were provided to the constructor
        /// </summary>
        public string[] StaticMappings { get; private set; }

        /// <summary>
        /// Creates a new ArgPipelineExtractor instance.
        /// </summary>
        /// <param name="staticMappings">The static string mappings to allow</param>
        public ArgPipelineExtractor(params string[] staticMappings)
        {
            this.StaticMappings = staticMappings == null ? new string[0] : staticMappings;
        }

        internal bool TryExtractObjectPropertyIntoCommandLineArgument(object o, CommandLineArgument argument, out string commandLineKey, out string commandLineValue)
        {
            if (ArgPipelineObjectMapper.CurrentMapper == null || ArgPipelineObjectMapper.CurrentMapper.TryExtractObjectPropertyIntoCommandLineArgument(o, argument, StaticMappings, out commandLineKey, out commandLineValue) == false)
            {
                return TryDefaultShredStaticObjectPropertyIntoCommandLineArgument(o, argument, out commandLineKey, out commandLineValue);
            }
            else
            {
                return true;
            }
        }

        private bool TryDefaultShredStaticObjectPropertyIntoCommandLineArgument(object o, CommandLineArgument argument, out string commandLineKey, out string commandLineValue)
        {
            var mapCandidates = argument.Aliases.Union(StaticMappings).Select(a => a.Replace("-", ""));

            var mapSuccessCandidate = (from p in o.GetType().GetProperties()
                                       where mapCandidates.Contains(p.Name, StringComparer.Create(System.Globalization.CultureInfo.InvariantCulture, true))
                                       select p).FirstOrDefault();

            if (mapSuccessCandidate != null)
            {
                commandLineKey = "-" + argument.DefaultAlias;
                commandLineValue = mapSuccessCandidate.GetValue(o, null) + "";
                return true;
            }
            else
            {
                commandLineKey = null;
                commandLineValue = null;
                return false;
            }
        }
    }
}
