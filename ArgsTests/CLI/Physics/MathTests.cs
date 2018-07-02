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
        public void TestCalculateLineOfSightLeftToRight()
        {
            var spaceTime = new SpaceTime(100, 100);
            spaceTime.QueueAction(() =>
            {
                var from = PowerArgs.Cli.Physics.Rectangular.Create(0, 0, 1, 1);
                var to = PowerArgs.Cli.Physics.Rectangular.Create(10, 0, 1, 1);
                var route = from.CalculateLineOfSight(to, 1);

                Assert.AreEqual(9, route.Steps.Count);
                Assert.AreEqual(0, route.Obstacles.Count);

                for(var i = 0; i < route.Steps.Count; i++)
                {
                    Assert.AreEqual(0, route.Steps[i].Top); // make sure I'm travelling horizontally
                    Assert.AreEqual(i + 1, route.Steps[i].Left); // make sure I moved by 1 towards the target
                }

                spaceTime.Stop();
            });

            spaceTime.Start().Wait();
        }

        [TestMethod]
        public void TestCalculateLineOfSightRightToLeft()
        {
            var spaceTime = new SpaceTime(100, 100);
            spaceTime.QueueAction(() =>
            {
                var from = PowerArgs.Cli.Physics.Rectangular.Create(10, 0, 1, 1);
                var to = PowerArgs.Cli.Physics.Rectangular.Create(0, 0, 1, 1);
                var route = from.CalculateLineOfSight(to, 1);

                Assert.AreEqual(9, route.Steps.Count);
                Assert.AreEqual(0, route.Obstacles.Count);

                for (var i = 0; i < route.Steps.Count; i++)
                {
                    Assert.AreEqual(0, route.Steps[i].Top); // make sure I'm travelling horizontally
                    Assert.AreEqual(9-i, route.Steps[i].Left); // make sure I moved by 1 towards the target
                }

                spaceTime.Stop();
            });

            spaceTime.Start().Wait();
        }
    }
}
