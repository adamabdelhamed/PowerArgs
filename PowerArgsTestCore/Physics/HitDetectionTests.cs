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
        public void TestHitDetectionHitRight()
        {
            var options = new HitDetectionOptions()
            {
                MovingObject = new RectF(0, 0, 1, 1),
                Obstacles = new RectF[]  { new RectF(10, 0, 1, 1) },
                Angle = 0,
                Visibility = 11,
            };
            var prediction = HitDetection.PredictHit(options);
            Assert.AreEqual(HitType.Obstacle, prediction.Type);
            Assert.AreEqual(prediction.ObstacleHitBounds, options.Obstacles[0]);
        }

        [TestMethod]
        public void TestHitDetectionHitLeft()
        {
            var options = new HitDetectionOptions()
            {
                MovingObject = new RectF(0, 0, 1, 1),
                Obstacles = new RectF[] { new RectF(-10, 0, 1, 1) },
                Angle = 180,
                Visibility = 21,
            };
            var prediction = HitDetection.PredictHit(options);
            Assert.AreEqual(HitType.Obstacle, prediction.Type);
            Assert.AreEqual(prediction.ObstacleHitBounds, options.Obstacles[0]);
        }

        [TestMethod]
        public void TestHitDetectionMissRightShortVis()
        {
            var options = new HitDetectionOptions()
            {
                MovingObject = new RectF(0, 0, 1, 1),
                Obstacles = new RectF[] { new RectF(10, 0, 1, 1) },
                Angle = 0,
                Visibility = 5,
            };
            var prediction = HitDetection.PredictHit(options);
            Assert.AreEqual(HitType.None, prediction.Type);
            Assert.AreNotEqual(prediction.ObstacleHitBounds, options.Obstacles[0]);
        }

        [TestMethod]
        public void TestHitDetectionMissLeftShortVis()
        {
            var options = new HitDetectionOptions()
            {
                MovingObject = new RectF(0, 0, 1, 1),
                Obstacles = new RectF[] { new RectF(-10, 0, 1, 1) },
                Angle = 180,
                Visibility = 5,
            };
            var prediction = HitDetection.PredictHit(options);
            Assert.AreEqual(HitType.None, prediction.Type);
            Assert.AreNotEqual(prediction.ObstacleHitBounds, options.Obstacles[0]);
        }

        [TestMethod]
        public void TestHitDetectionMissRightNoCollide()
        {
            var options = new HitDetectionOptions()
            {
                MovingObject = new RectF(0, 0, 1, 1),
                Obstacles = new RectF[] { new RectF(-10, 0, 1, 1) },
                Angle = 0,
                Visibility = 11,
            };
            var prediction = HitDetection.PredictHit(options);
            Assert.AreEqual(HitType.None, prediction.Type);
            Assert.AreNotEqual(prediction.ObstacleHitBounds, options.Obstacles[0]);
        }

        [TestMethod]
        public void TestHitDetectionMissLeftNoCollide()
        {
            var options = new HitDetectionOptions()
            {
                MovingObject = new RectF(0, 0, 1, 1),
                Obstacles = new RectF[] { new RectF(10, 0, 1, 1) },
                Angle = 180,
                Visibility = 21,
            };
            var prediction = HitDetection.PredictHit(options);
            Assert.AreEqual(HitType.None, prediction.Type);
            Assert.AreNotEqual(prediction.ObstacleHitBounds, options.Obstacles[0]);
        }


        [TestMethod]
        public async Task TestHitDetectionSmallOverlaps() => await PhysicsTest.Test(50,25, TestContext, async (app, stPanel) =>
        {
            var e1 = SpaceTime.CurrentSpaceTime.Add(new SpacialElement(1, 1, 5, 5));
            var e2 = SpaceTime.CurrentSpaceTime.Add(new SpacialElement(1,1, 5.9f, 5.9f));

            var options = new HitDetectionOptions(e2, new ICollider[] { e1 });
            options.Angle = 225;
            options.Visibility = 10;

            var expectHit = HitDetection.PredictHit(options);
            Assert.AreEqual(e1, expectHit.ColliderHit);

            var options2 = new HitDetectionOptions(e2, new ICollider[] { e1 });
            options.Angle = 45;
            options.Visibility = 10;
            var expectMiss = HitDetection.PredictHit(options2);
            Assert.IsTrue(expectMiss.Type == HitType.None);
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
            while(eye.Bounds.Right < SpaceTime.CurrentSpaceTime.Width)
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
                    var angle = eye.Bounds.CalculateAngleTo(obstacle.Bounds);
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
