using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading;
using PowerArgs.Cli.Physics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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

        [TestMethod]
        public async Task TestHitDetection() => await PhysicsTest.Test(50, 25, TestContext, async (app, stPanel) =>
        {
            for(var x = 4; x < SpaceTime.CurrentSpaceTime.Width-4; x+=10)
            {
                for (var y = 4; y < SpaceTime.CurrentSpaceTime.Height - 4; y += 5)
                {
                    var e1 = SpaceTime.CurrentSpaceTime.Add(new SpacialElement(x: x, y: y, w:2, h:1) { BackgroundColor = RGB.Gray });
                }
            }

            var eye =  SpaceTime.CurrentSpaceTime.Add(new SpacialElement(x: 0, y: 12, w: 2, h: 1) { BackgroundColor = RGB.Red });

            var lines = new List<SpacialElement>();
            while(eye.Right() < SpaceTime.CurrentSpaceTime.Width)
            {
                eye.MoveBy(.05f, 0);
                foreach (var line in lines) { line.Lifetime.Dispose(); };
                lines.Clear();

                foreach (var obstacle in SpaceTime.CurrentSpaceTime.Elements.Where(e => e != eye).ToArray())
                {
                    obstacle.BackgroundColor = RGB.Gray;
                    obstacle.SizeOrPositionChanged.Fire();
                }

                foreach (var obstacle in SpaceTime.CurrentSpaceTime.Elements.Where(e => e != eye).ToArray())
                {
                    var angle = eye.Center().CalculateAngleTo(obstacle.Center());
                    var los = HitDetection.HasLineOfSight(eye, obstacle);

                    if(los)
                    {
                        obstacle.BackgroundColor = RGB.Green;
                        obstacle.SizeOrPositionChanged.Fire();
                    }
                }

                await Task.Yield();
            }
        });
    }
}
