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
            Assert.AreEqual(45, 0.AddToAngle(45));
            Assert.AreEqual(1, 360.AddToAngle(1));
            Assert.AreEqual(359,  0.AddToAngle(-1));
            Assert.AreEqual(359, 360.AddToAngle(-1));
            
        }
    }
}
