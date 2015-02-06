using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.CLI
{
    [TestClass]
    public class PromptTests
    {
        [TestInitialize]
        public void Setup()
        {
            ConsoleInDriver.Instance.Attach();
        }

        [TestCleanup]
        public void Cleanup()
        {
            ConsoleInDriver.Instance.Detach();
        }

        [TestMethod]
        public void IsUserSure()
        {
            ConsoleInDriver.Instance.DriveLine("y");
            ConsoleInDriver.Instance.DriveLine("n");
            ConsoleInDriver.Instance.DriveLine("notagoodanswer");
            ConsoleInDriver.Instance.DriveLine("y");

            var cli = new Cli();
            var firstAnswer = cli.IsUserSure("Dude this is dangerous");
            var secondAnswer = cli.IsUserSure("Dude this is dangerous");
            var thirdAnswer = cli.IsUserSure("Dude this is dangerous");

            Assert.IsTrue(firstAnswer);
            Assert.IsFalse(secondAnswer);
            Assert.IsTrue(thirdAnswer);
        }
    }
}
