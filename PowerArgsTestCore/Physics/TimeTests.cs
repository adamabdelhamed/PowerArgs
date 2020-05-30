using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
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
            t.InvokeNextCycle(async() =>
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

            t.Start().Wait();
            Assert.AreEqual(20, assertions);
        }


        [TestMethod]
        public void TestTimePerf()
        {
            var t = new SpaceTime(80, 30, TimeSpan.FromSeconds(.05));
            var now = TimeSpan.Zero;
            t.Invoke(async () =>
            {
                for(var i = 0; i < 10000; i++)
                {
                    t.Add(new DummyFunction());
                }

                while (t.Now < TimeSpan.FromSeconds(100000))
                {
                    Assert.AreEqual(now, t.Now);
                    now = now.Add(t.Increment);
                    await t.YieldAsync();
                }
                t.Stop();
            });

            t.Start().Wait();
        }
    }

    public class DummyFunction : TimeFunction
    {
  
    }
}
