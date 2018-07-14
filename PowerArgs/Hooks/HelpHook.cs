using System;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// A hook that lets you turn a boolean property into a command line switch that short circuits processing and displays help.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class HelpHook : ArgHook
    {
        /// <summary>
        /// A type that should implement IUsageTemplateProvider.  When specified the help hook will use the GenerateUsageFromTemplate function rather than the obsolete GenerateStyledUsage function.
        /// </summary>
        public Type  UsageTemplateProviderType { get; set; }

        /// <summary>
        /// If true (which it is by default) the hook will write the help after the target property is populated.  If false, processing will still stop, but
        /// the help will not be written (yoy will have to do it yourself).
        /// </summary>
        public bool WriteHelp { get; set; }

        /// <summary>
        /// An event that fires when the hook writes usage to the console
        /// </summary>
        public event Action<ConsoleString> UsageWritten;

        private CommandLineArgument target;

        private bool iDidTheCancel;

        /// <summary>
        /// Creates a new help hook instance
        /// </summary>
        public HelpHook()
        {
            WriteHelp = true;
            UsageTemplateProviderType = typeof(DefaultConsoleUsageTemplateProvider);
            this.AfterCancelPriority = 0; // we want this to run last
        }

        /// <summary>
        /// Makes sure the target is a boolean
        /// </summary>
        /// <param name="context">Context passed by the parser</param>
        public override void BeforePopulateProperty(ArgHook.HookContext context)
        {
            base.BeforePopulateProperty(context);
            this.target = context.CurrentArgument;
            if (context.CurrentArgument.ArgumentType != typeof(bool))
            {
                throw new InvalidArgDefinitionException(typeof(HelpHook).Name +" attributes can only be used with boolean properties or parameters");
            }
        }

        /// <summary>
        /// This gets called after the target property is populated.  It cancels processing.
        /// </summary>
        /// <param name="context">Context passed by the parser</param>
        public override void AfterPopulateProperty(HookContext context)
        {
            iDidTheCancel = false;
            base.AfterPopulateProperty(context);
            if (context.CurrentArgument.RevivedValue is bool &&
                ((bool)context.CurrentArgument.RevivedValue) == true)
            {
                iDidTheCancel = true;
                context.CancelAllProcessing();
            }
        }

        /// <summary>
        /// Writes the help as long as WriteHelp is true
        /// </summary>
        /// <param name="context">Context passed by the parser</param>
        public override void AfterCancel(ArgHook.HookContext context)
        {
            base.AfterCancel(context);
            if (iDidTheCancel == false) return;
            if (WriteHelp == false) return;
            var usage = UsageTemplateProvider.GetUsage(UsageTemplateProviderType, context.Definition);
            usage.Write();
            UsageWritten?.Invoke(usage);
        }
    }
}
