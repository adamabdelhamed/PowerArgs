using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    public class BasicUsageArgs
    {
        [ArgPosition(0)]
        [ArgDescription("A string arg")]
        public string StringArgs { get; set; }
        [ArgDescription("An int arg")]
        public int IntArgs { get; set; }
    }

    [TestClass]
    public class UsageTests
    {
        [TestMethod]
        public void TestUsageWithoutTypeAndPosition()
        {
            var usage = ArgUsage.GetUsage<BasicUsageArgs>("test", new ArgUsageOptions() 
            {
                ShowType = false,
                ShowPosition=false,
            });

            Assert.IsFalse(usage.Contains("TYPE"));
            Assert.IsFalse(usage.Contains("POSITION"));
        }

        [TestMethod]
        public void TestUsageWithTypeAndPosition()
        {
            var usage = ArgUsage.GetUsage<BasicUsageArgs>("test");

            Assert.IsTrue(usage.Contains("TYPE"));
            Assert.IsTrue(usage.Contains("POSITION"));
        }

        [TestMethod]
        public void TestUsageWithTypeAndNotPosition()
        {
            var usage = ArgUsage.GetUsage<BasicUsageArgs>("test", new ArgUsageOptions()
            {
                ShowType = true,
                ShowPosition = false,
            });

            Assert.IsTrue(usage.Contains("TYPE"));
            Assert.IsFalse(usage.Contains("POSITION"));
        }


        [TestMethod]
        public void TestUsageWithNoTypeAndWithPosition()
        {
            var usage = ArgUsage.GetUsage<BasicUsageArgs>("test", new ArgUsageOptions()
            {
                ShowType = false,
                ShowPosition = true,
            });

            Assert.IsFalse(usage.Contains("TYPE"));
            Assert.IsTrue(usage.Contains("POSITION"));
        }
    }
}
