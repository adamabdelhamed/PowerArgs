using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs.Preview
{
    /// <summary>
    /// An attribute that lets you declare that the target class implements a pipeline action stage like the $filter stage that's
    /// provided by default.  Use this when building your own action stages.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ArgPipelineActionStage : Attribute
    {
        private static Dictionary<string, Type> registeredActionStageTypes = RegisterBuiltInActionStageTypes();

        /// <summary>
        /// Gets the key to this action stage (e.g. "$filter" for the $filter stage)
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Creates a new stage attribute with the given key.  If the given value doesn't start with a '$' then it
        /// will be added for you.  The key will also be converted to lowercase.
        /// </summary>
        /// <param name="key">The action key (e.g. "$filter" for the $filter stage)</param>
        public ArgPipelineActionStage(string key)
        {
            if (key.StartsWith(ArgPipeline.PipelineStageActionIndicator) == false)
            {
                key = ArgPipeline.PipelineStageActionIndicator + key;
            }
            key = key.ToLower();
            this.Key = key;
        }

        /// <summary>
        /// Searches the given assembly for types that implement PipelineStage and have the [ArgPipelineActionStage] attribute.  It then
        /// registers the given stage with the system so that they can be used by end users in their pipelines.
        /// </summary>
        /// <param name="a">The assembly to search</param>
        public static void RegisterActionStages(Assembly a)
        {
            foreach(var result in SearchAssemblyForActionStages(a))
            {
                AssertKeyNotAlreadyRegistered(result.Key);
                registeredActionStageTypes.Add(result.Key, result.Value);
            }
        }

        /// <summary>
        /// Explicitly registers an action stage given a key and a corresponding type
        /// </summary>
        /// <param name="key">The action key (e.g. "$filter" for the $filter stage)</param>
        /// <param name="type">The type that implements the stage</param>
        public static void RegisterActionStage(string key, Type type)
        {
            if (type.IsSubclassOf(typeof(PipelineStage)) == false)
            {
                throw new InvalidArgDefinitionException("The type '" + type.FullName + "' does not implement " + typeof(PipelineStage).FullName);
            }

            key = new ArgPipelineActionStage(key).Key;
            AssertKeyNotAlreadyRegistered(key);
            registeredActionStageTypes.Add(key, type);
        }

        internal static bool TryCreateActionStage(string[] commandLine, out PipelineStage stage)
        {
            Type stageType;
            if (registeredActionStageTypes.TryGetValue(commandLine[0].ToLower(), out stageType) == false)
            {
                stage = null;
                return false;
            }
            else
            {
                stage = (PipelineStage)Activator.CreateInstance(stageType, new object[] { commandLine.Skip(1).ToArray() });
                return true;
            }
        }

        private static void AssertKeyNotAlreadyRegistered(string key)
        {
            AssertKeyNotAlreadyRegistered(key, registeredActionStageTypes);
        }

        private static void AssertKeyNotAlreadyRegistered(string key, Dictionary<string, Type> dictionary)
        {
            if (dictionary.ContainsKey(key))
            {
                throw new InvalidArgDefinitionException("The action stage '" + registeredActionStageTypes[key].FullName + "' is already registered for key '" + key + "'");
            }
        }

        private static Dictionary<string, Type> RegisterBuiltInActionStageTypes()
        {
            return SearchAssemblyForActionStages(Assembly.GetExecutingAssembly());
        }

        private static Dictionary<string, Type> SearchAssemblyForActionStages(Assembly a)
        {
            Dictionary<string, Type> ret = new Dictionary<string, Type>();
            foreach (var t in a.GetTypes().Where(t => t.HasAttr<ArgPipelineActionStage>() && t.IsSubclassOf(typeof(PipelineStage))))
            {
                var key = t.Attr<ArgPipelineActionStage>().Key;
                AssertKeyNotAlreadyRegistered(key, ret);
                ret.Add(key, t);
            }

            return ret;
        }
    }
}
