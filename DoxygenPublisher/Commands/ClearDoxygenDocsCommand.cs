using PowerArgs;

namespace DoxygenPublisher
{
    [ArgActions]
    public class ClearDoxygenDocsCommand
    {
        [ArgActionMethod, ArgDescription("Deletes doxygen docs for the given version of PowerArgs from the web")]
        public static void ClearDoxygenDocs(ClearDoxygenDocsArgs args)
        {
            Helpers.ClearContainer(args.Container, args.Version);
        } 
    }
}

