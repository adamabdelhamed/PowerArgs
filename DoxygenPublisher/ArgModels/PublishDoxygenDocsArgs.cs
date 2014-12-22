using PowerArgs;

namespace DoxygenPublisher
{
    public class PublishDoxygenDocsArgs : StorageContainerScopedArgs
    {
        [ArgRequired(PromptIfMissing = true), StickyArg, ArgExistingFile, ArgDescription("The path to the doxygen configuration file")]
        public string DoxyFile { get; set; }
    }
}
