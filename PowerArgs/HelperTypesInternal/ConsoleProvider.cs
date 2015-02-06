
namespace PowerArgs
{
    /// <summary>
    /// The console provider that is used across all of Powerargs
    /// </summary>
    public static class ConsoleProvider
    {
        /// <summary>
        /// Gets or sets the console implementation that is targeted by PowerArgs.  By default, PowerArgs uses the standard system console.  In theory,
        /// you can implement a custom version of IConsoleProvider and plug it in here.  Everything should work, but it has not been attempted.  Proceed with caution.
        /// </summary>
        public static IConsoleProvider Current = new StdConsoleProvider();
    }
}
