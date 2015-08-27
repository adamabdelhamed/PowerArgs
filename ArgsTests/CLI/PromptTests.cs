using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;

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
            ConsoleProvider.Current = new TestConsoleProvider("y{enter}n{enter}notagoodanswer{enter}y");

            var cli = new CliHelper();
            var firstAnswer = cli.IsUserSure("Dude this is dangerous");
            var secondAnswer = cli.IsUserSure("Dude this is dangerous");
            var thirdAnswer = cli.IsUserSure("Dude this is dangerous");

            Assert.IsTrue(firstAnswer);
            Assert.IsFalse(secondAnswer);
            Assert.IsTrue(thirdAnswer);
        }
    }
}
