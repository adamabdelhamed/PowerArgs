using PowerArgs;

namespace DoxygenPublisher
{
    [TabCompletion(HistoryToSave = 100, REPL=true, REPLWelcomeMessage="PowerArgs documentation publisher REPL.  Type 'quit' to exit"), StorageResetHook, ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling), ArgActionResolver, ArgDescription("A command line program that is used to generate, publish, and manage PowerArgs' reference documentation to the cloud.")]
    class Program
    {
        [HelpHook, ArgShortcut("-?"), ArgDescription("Displays help documentation")]
        public bool Help { get; set; }

        private static void Main(string[] args) 
        {
            Args.InvokeAction<Program>(args); 
        }
    }
}