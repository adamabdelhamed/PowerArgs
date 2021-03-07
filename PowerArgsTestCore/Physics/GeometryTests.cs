using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ArgsTests.CLI.Physics
{

 

    [TestClass]
    [TestCategory(Categories.Physics)]
    public class GeometryTests
    {
        public TestContext TestContext { get; set; }

 

        [TestMethod]
        public async Task TestNormalizedProximity()
        {
            var app = new CliTestHarness(TestContext, 40, 40, true);
            app.InvokeNextCycle(async () =>
            {
                var a = app.LayoutRoot.Add(new ConsoleControl() { Background = ConsoleColor.Red, Width = 1, Height = 1, X = 0, Y = 0 });
                var b = app.LayoutRoot.Add(new ConsoleControl() { Background = ConsoleColor.Green, Width = 1, Height = 1, X = 39, Y = 39 });
                var d = Geometry.CalculateNormalizedDistanceTo(a, b);
                Console.WriteLine(d);
                await app.PaintAndRecordKeyFrameAsync();
                app.Stop();
            });

            await app.Start();
            app.AssertThisTestMatchesLKG();
        }

        [TestMethod]
        public void TestAngleGeometry()
        {
            Assert.AreEqual(45, 0.AddToAngle(45));
            Assert.AreEqual(1, 360.AddToAngle(1));
            Assert.AreEqual(359, 0.AddToAngle(-1));
            Assert.AreEqual(359, 360.AddToAngle(-1));
            Assert.AreEqual(0, 0.DiffAngle(360));
            Assert.AreEqual(1, 1.DiffAngle(360));
            Assert.AreEqual(45, 0.DiffAngle(45));
            Assert.AreEqual(0, RectangularF.Create(0, 0, 1, 1).CalculateAngleTo(RectangularF.Create(1, 0, 1, 1)));
            Assert.AreEqual(45, RectangularF.Create(0, 0, 1, 1).CalculateAngleTo(RectangularF.Create(1, 1, 1, 1)));
            Assert.AreEqual(90, RectangularF.Create(0, 0, 1, 1).CalculateAngleTo(RectangularF.Create(0, 1, 1, 1)));
            Assert.AreEqual(135, RectangularF.Create(1, 0, 1, 1).CalculateAngleTo(RectangularF.Create(0, 1, 1, 1)));
            Assert.AreEqual(180, RectangularF.Create(1, 0, 1, 1).CalculateAngleTo(RectangularF.Create(0, 0, 1, 1)));
            Assert.AreEqual(225, RectangularF.Create(1, 1, 1, 1).CalculateAngleTo(RectangularF.Create(0, 0, 1, 1)));
            Assert.AreEqual(270, RectangularF.Create(1, 1, 1, 1).CalculateAngleTo(RectangularF.Create(1, 0, 1, 1)));
            Assert.AreEqual(315, RectangularF.Create(0, 1, 1, 1).CalculateAngleTo(RectangularF.Create(1, 0, 1, 1)));
        }

        [TestMethod]
        public void TestAngleBisect()
        {
            Assert.AreEqual(0, 315f.Bisect(45));
            Assert.AreEqual(90, 1f.Bisect(179));
            Assert.AreEqual(270, 359f.Bisect(181));
            Assert.AreEqual(22.5f, 0f.Bisect(45));
            Assert.AreEqual(90, 0f.Bisect(180));
            Assert.AreEqual(270, 180f.Bisect(360));
        }

        [TestMethod]
        public void TestDistanceGeometry()
        {
            Assert.AreEqual(0, RectangularF.Create(0, 0, 1, 1).CalculateDistanceTo(RectangularF.Create(0, 0, 1, 1)));
            Assert.AreEqual(0, RectangularF.Create(0, 0, 1, 1).CalculateDistanceTo(RectangularF.Create(1, 1, 1, 1)));
            Assert.AreEqual(1, RectangularF.Create(0, 0, 1, 1).CalculateDistanceTo(RectangularF.Create(2, 0, 1, 1)));
        }
    }
}
