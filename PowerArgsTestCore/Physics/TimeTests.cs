using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ArgsTests.CLI.Physics
{
    [TestClass]
    [TestCategory(Categories.Physics)]
    public class TimeTests
    {
 
        [TestMethod]
        public void TestYieldDoesOneIncrementAtATime()
        {
            var t = new Time(TimeSpan.FromSeconds(.05));
            var now = TimeSpan.Zero;
            var assertions = 0;
            t.QueueAction(async() =>
            {
                while(t.Now < TimeSpan.FromSeconds(1))
                {
                    Assert.AreEqual(now, t.Now);
                    assertions++;
                    now = now.Add(t.Increment);
                    await t.YieldAsync();
                }
                t.Stop();
            });

            t.Start(nameof(TestYieldDoesOneIncrementAtATime)).Wait();
            Assert.AreEqual(20, assertions);
        }
    }
}
