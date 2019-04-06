using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ArgsTests.CLI.Physics
{
    [TestClass]
    public class MathTests
    {
        public TestContext TestContext { get; set; }

 

        [TestMethod]
        public void TestAngleMath()
        {
            Assert.AreEqual(45, SpaceExtensions.AddToAngle(0, 45));
            Assert.AreEqual(1, SpaceExtensions.AddToAngle(360, 1));
            Assert.AreEqual(359, SpaceExtensions.AddToAngle(0, -1));
            Assert.AreEqual(359, SpaceExtensions.AddToAngle(360, -1));
            
        }
    }
}
