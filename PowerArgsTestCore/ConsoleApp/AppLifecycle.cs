using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading.Tasks;

namespace ArgsTests.CLI.Apps
{
    [TestClass]
    [TestCategory(Categories.ConsoleApp)]
    public class AppLifecycle
    {
        [TestMethod]
        public void PumpFailurePreservesStack()
        {
            var testCli = new CliUnitTestConsole(80, 4);
            ConsoleProvider.Current = testCli;
            ConsoleApp app = new ConsoleApp();
            var promise = app.Start();

            app.InvokeNextCycle(() =>
            {
                throw new FormatException("Some fake exception");
            });

            try
            {
                promise.Wait();
                Assert.Fail("An exception should have been thrown");
            }
            catch (PromiseWaitException ex)
            {
                Assert.AreEqual(typeof(FormatException), ex.InnerException.GetType());
                Assert.AreEqual("Some fake exception", ex.InnerException.Message);
            }
        }
    }
}
