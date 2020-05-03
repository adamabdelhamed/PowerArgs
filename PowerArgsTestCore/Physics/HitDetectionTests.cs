using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading;
using PowerArgs.Cli.Physics;
using System.Threading.Tasks;
using PowerArgs.Games;

namespace ArgsTests.CLI.Physics
{
    [TestClass]
    [TestCategory(Categories.Physics)]
    public class HitDetectionTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task TestHitDetectionSmallOverlaps() => await PhysicsTest.Test(50,25, TestContext, async (app, stPanel) =>
        {
            var e1 = SpaceTime.CurrentSpaceTime.Add(new SpacialElement(1, 1, 5, 5));
            var e2 = SpaceTime.CurrentSpaceTime.Add(new SpacialElement(1,1, 5.9f, 5.9f));
            var expectHit = HitDetection.PredictHit(new HitDetectionOptions() 
            { 
                MovingObject = e2, 
                Obstacles = new SpacialElement[] { e1 }, 
                Angle = 225,
                Visibility = 10
            });
            Assert.AreEqual(e1, expectHit.ObstacleHit);

            var expectMiss = HitDetection.PredictHit(new HitDetectionOptions()
            {
                MovingObject = e2,
                Obstacles = new SpacialElement[] { e1 },
                Angle = 45,
                Visibility = 10
            });
            Assert.AreEqual(null, expectMiss.ObstacleHit);
        });
    }
}
