
using PowerArgs;
namespace DoxygenPublisher
{
    public class ClearDoxygenDocsArgs : StorageContainerScopedArgs
    {
        [ArgRequired(PromptIfMissing = true), ArgDescription("The version of the docs to remove")]
        public string Version { get; set; }
    }
}
