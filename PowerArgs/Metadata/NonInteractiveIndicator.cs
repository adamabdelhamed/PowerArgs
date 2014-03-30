using System;
using System.Linq;
namespace PowerArgs
{
    /// <summary>
    /// An attribute that can be specified on a boolean argument to indicate a non interactive session.  
    /// When used, it sets IsNonInteractive on the current definition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class NonInteractiveIndicator : ArgHook
    {
        /// <summary>
        /// Creates a new NonInteractiveIndicator attribute
        /// </summary>
        public NonInteractiveIndicator()
        {
            this.BeforeParsePriority = new TabCompletion().BeforeParsePriority + 1;
        }

        /// <summary>
        /// If the current argument is a boolean and it is specified on the command line then
        /// this hook sets the IsNonInteractive flag on the current argument definition.
        /// </summary>
        /// <param name="context"></param>
        public override void BeforeParse(ArgHook.HookContext context)
        {
            if(context.CurrentArgument.ArgumentType != typeof(bool))
            {
                throw new InvalidArgDefinitionException(GetType().Name + " can only be used on boolean arguments");
            }
 
            for (int i = 0; i < context.CmdLineArgs.Length; i++ )
            {
                var arg = context.CmdLineArgs[i];
                string key;

                if (ArgParser.TryParseKey(arg, out key))
                {
                    var nextArg = i == context.CmdLineArgs.Length - 1 ? "" : context.CmdLineArgs[i + 1].ToLower();

                    // TODO - Find a better way to detect explicit 'false'
                    if (context.CurrentArgument.IsMatch(key) && nextArg != "false" && nextArg != "0")
                    {
                        context.Definition.IsNonInteractive = true;
                    }
                }
            }
        }
    }
}
