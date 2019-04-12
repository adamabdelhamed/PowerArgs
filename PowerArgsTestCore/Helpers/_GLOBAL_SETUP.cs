using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class _GLOBAL_SETUP
    {
        [AssemblyInitialize]
        public static void GlobalSetup(TestContext context)
        {
            ConsoleProvider.Current = new TestConsoleProvider();
        }
    }
}
