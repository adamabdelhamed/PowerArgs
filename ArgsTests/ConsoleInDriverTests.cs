using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class ConsoleInDriverTests
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
        public void ConsoleInDriverDriveLine()
        {
            var expected = "The line";
            ConsoleInDriver.Instance.DriveLine(expected);
            var actual = Console.ReadLine();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ConsoleInDriverReadKey()
        {
            var expected = "The line";
            ConsoleInDriver.Instance.DriveLine(expected);
            var actual = Console.Read();
            Assert.AreEqual(expected[0], (char)actual);
        }
    }
}
