using System;

namespace PowerArgs.Preview
{
    /// <summary>
    /// Metadata that designates a particular command line argument as an action's direct pipeline target.  When specified any and all pipeline input will
    /// be used to directly populate the given argument.  That means that any validators and revivers that would normally run on the string representation of the
    /// argument will not run since the argument will already be 'revived' by virtue of being sent through the pipeline.
    /// </summary>
    public class ArgPipelineTarget : ArgHook, ICommandLineArgumentMetadata
    {
        /// <summary>
        /// If true then the target argument can only be populated via the pipeline and therefore should be omitted from usage documentation
        /// and the target argument does not need to be revivable from a string.
        /// </summary>
        public bool PipelineOnly { get; set; }

        /// <summary>
        /// Creates a new ArgPipelineTarget instance.
        /// </summary>
        public ArgPipelineTarget()
        {
            PipelineOnly = true;
        }

        /// <summary>
        /// Makes sure that 'MustBeRevivable' is false for pipeline only arguments
        /// </summary>
        /// <param name="context">the processing context</param>
        public override void BeforeValidateDefinition(ArgHook.HookContext context)
        {
            EnforceOmittingFromUsageIfPipelineOnly(context);
            if(PipelineOnly)
            {
                context.CurrentArgument.MustBeRevivable = false;
            }
        }

        /// <summary>
        /// Makes sure that the argument is omitted from usage documentation if it can only be populated from the pipeline
        /// </summary>
        /// <param name="context">the processing context</param>
        public override void BeforePrepareUsage(HookContext context)
        {
            EnforceOmittingFromUsageIfPipelineOnly(context);
        }

        private void EnforceOmittingFromUsageIfPipelineOnly(HookContext context)
        {
            if (PipelineOnly == true && context.CurrentArgument.Metadata.HasMeta<OmitFromUsageDocs>() == false)
            {
                context.CurrentArgument.Metadata.Add(new OmitFromUsageDocs());
            }
        }
    }
}
