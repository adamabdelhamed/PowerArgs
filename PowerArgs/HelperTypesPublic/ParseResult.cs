using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// The raw parse result that contains the dictionary of values that were parsed
    /// </summary>
    public class ParseResult
    {
        /// <summary>
        /// Dictionary of values that were either in the format -key value or /key:value on
        /// the command line.
        /// </summary>
        public Dictionary<string, string> ExplicitParameters { get; private set; }

        /// <summary>
        /// Dictionary of values that were implicitly specified by position where the key is the position (e.g. 0)
        /// and the value is the actual parameter value.
        /// 
        /// Example command line:  Program.exe John Smith
        /// 
        /// John would be an implicit parameter at position 0.
        /// Smith would be an implicit parameter at position 1.
        /// </summary>
        public Dictionary<int, string> ImplicitParameters { get; private set; }

        /// <summary>
        /// This is only populated for programs that support multiple command line arguments mapping to a single logical argument.  For example, 
        /// you may have an argument called -files that you would want to be used like this: -files file1 file2 file3.
        /// In this case, this dictionary would contain an entry with key 'files' and values 'file2, file3'.  Note that file1 will be populated
        /// in ExplicitParameters for legacy reasons
        /// </summary>
        public Dictionary<string, List<string>> AdditionalExplicitParameters { get; private set; }

        internal ParseResult()
        {
            ExplicitParameters = new Dictionary<string, string>(); 
            ImplicitParameters = new Dictionary<int, string>();
            AdditionalExplicitParameters = new Dictionary<string, List<string>>();  
        }

        internal void AddAdditionalParameter(string key, string value)
        {
            List<string> target;
            if(AdditionalExplicitParameters.TryGetValue(key, out target) == false)
            {
                target = new List<string>();
                target.Add(value);
                AdditionalExplicitParameters.Add(key, target);
            }
            else
            {
                target.Add(value);
            }
        }

        internal bool TryGetAndRemoveAdditionalExplicitParameters(CommandLineArgument argument, out List<string> result)
        {
            foreach(var knownKey in AdditionalExplicitParameters.Keys)
            {
                if(argument.IsMatch(knownKey))
                {
                    result = AdditionalExplicitParameters[knownKey];
                    AdditionalExplicitParameters.Remove(knownKey);
                    return true;
                }
            }
            result = null;
            return false;
        }
    }
}
