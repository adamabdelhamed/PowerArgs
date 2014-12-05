using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    internal class ActionAndArgumentSmartTabCompletionSource : ISmartTabCompletionSource
    {
        private SimpleTabCompletionSource actionSource;
        private SimpleTabCompletionSource globalArgumentSource;
        private Dictionary<CommandLineAction, SimpleTabCompletionSource> actionSpecificArgumentSources;

        public bool TryComplete(TabCompletionContext context, out string completion)
        {
            if(actionSource == null)
            {
                actionSource = new SimpleTabCompletionSource(FindActions(context.Definition)) { MinCharsBeforeCyclingBegins = 0 };
                globalArgumentSource = new SimpleTabCompletionSource(FindGlobalArguments(context.Definition)) { MinCharsBeforeCyclingBegins = 0 };
                actionSpecificArgumentSources = FindActionSpecificSources(context.Definition);
            }

            // if this is the first token and the definition contains actions then try to auto complete an action name
            if(string.IsNullOrEmpty(context.PreviousToken) && context.Definition.Actions.Count > 0)
            {
                return actionSource.TryComplete(context, out completion);   
            }
            // if there is no action in context and no argument in context then try to auto complete global argument names
            else if(context.TargetAction == null && context.TargetArgument == null)
            {
                return globalArgumentSource.TryComplete(context, out completion);
            }
            // if there is an action in context and not argument in context then try to complete action specific argument names and then globals
            else if(context.TargetAction != null && context.TargetArgument == null)
            {
                var actionSpecificSource = actionSpecificArgumentSources[context.TargetAction];
                if (actionSpecificSource.TryComplete(context, out completion))
                {
                    return true;
                }
                else
                {
                    return globalArgumentSource.TryComplete(context, out completion);
                }
            }
            else
            {
                completion = null;
                return false;
            }
        }

        private Dictionary<CommandLineAction, SimpleTabCompletionSource> FindActionSpecificSources(CommandLineArgumentsDefinition definition)
        {
            var ret = new Dictionary<CommandLineAction, SimpleTabCompletionSource>();

            var argIndicator = "-";

            foreach (var action in definition.Actions)
            {
                List<string> arguments = new List<string>();
                foreach (var argument in action.Arguments)
                {

                    if (argument.ArgumentType == typeof(SecureStringArgument))
                    {
                        continue;
                    }

                    arguments.Add(argIndicator + argument.Aliases.First());
                }
                ret.Add(action, new SimpleTabCompletionSource(arguments) { MinCharsBeforeCyclingBegins = 0 });
            }

            return ret;
        }

        private List<string> FindActions(CommandLineArgumentsDefinition definition)
        {
            List<string> ret = new List<string>();

            foreach (var action in definition.Actions)
            {
                var name = action.Aliases.First();

                if (name.EndsWith(Constants.ActionArgConventionSuffix))
                {
                    name = name.Substring(0, name.Length - Constants.ActionArgConventionSuffix.Length);
                }

                if (action.IgnoreCase)
                {
                    ret.Add(name.ToLower());
                }
                else
                {
                    ret.Add(name);
                }
            }

            ret = ret.Distinct().ToList();
            return ret;
        }

        private List<string> FindGlobalArguments(CommandLineArgumentsDefinition definition)
        {
            List<string> ret = new List<string>();

            var argIndicator = "-";

            foreach (var argument in definition.Arguments)
            {
                if(argument.ArgumentType == typeof(SecureStringArgument))
                {
                    continue;
                }

                ret.Add(argIndicator + argument.Aliases.First());
            }

            return ret;
        }  
    }
}
