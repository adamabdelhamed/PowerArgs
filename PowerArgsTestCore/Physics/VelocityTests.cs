using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading;
using PowerArgs.Cli.Physics;
using System.Threading.Tasks;

namespace ArgsTests.CLI.Physics
{
    [TestClass]
    [TestCategory(Categories.Physics)]
    public class VelocityTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task TestCantGoThroughWallsRight() => await PhysicsTest.Test(50,25, TestContext, async (app, stPanel) =>
        {
            await TestCantGoThroughWalls(0, app, stPanel);
        });

        [TestMethod]
        public async Task TestCantGoThroughWallsLeft() => await PhysicsTest.Test(50, 25, TestContext, async (app, stPanel) =>
        {
            await TestCantGoThroughWalls(180, app, stPanel);
        });

        [TestMethod]
        public async Task TestCantGoThroughWallsUp() => await PhysicsTest.Test(50, 25, TestContext, async (app, stPanel) =>
        {
            await TestCantGoThroughWalls(270, app, stPanel);
        });

        [TestMethod]
        public async Task TestCantGoThroughWallsDown() => await PhysicsTest.Test(50, 25, TestContext, async (app, stPanel) =>
        {
            await TestCantGoThroughWalls(90, app, stPanel);
        });

        private async Task TestCantGoThroughWalls(Angle a, CliTestHarness app, SpaceTimePanel stPanel)
        {
            var st = stPanel.SpaceTime;
            var wall = st.Add(new SpacialElement() { BackgroundColor = RGB.DarkRed });
            LocF movingObjectLocation;
            float movementAngle;
            float expected;
            Func<SpacialElement,float> actual;
            if(a == 0)
            {
                movingObjectLocation = new LocF((int)(st.Width * .25f), st.Height * .5f - .5f);
                movementAngle = 0;
                wall.ResizeTo(.1f, st.Height);
                wall.MoveTo((int)(st.Width * .75f), 0);

                expected = wall.Left;
                actual = m => m.Bounds.Right;
            }
            else if(a == 180)
            {
                movingObjectLocation = new LocF((int)(st.Width * .75f), st.Height * .5f - .5f);
                movementAngle = 180;
                wall.ResizeTo(.1f, st.Height);
                wall.MoveTo((int)(st.Width * .25f)-.1f, 0);

                expected = wall.Bounds.Right;
                actual = m => m.Left;
            }
            else if (a == 270)
            {
                movingObjectLocation = new LocF(st.Width * .5f - .5f, (int)(st.Height * .75f));
                movementAngle = 270;
                wall.ResizeTo(st.Width, .1f);
                wall.MoveTo(0, (int)(st.Height * .25f)-.1f);

                expected = wall.Bounds.Bottom;
                actual = m => m.Top;
            }
            else if (a == 90)
            {
                movingObjectLocation = new LocF(st.Width * .5f - .5f, (int)(st.Height * .25f));
                movementAngle = 90;
                wall.ResizeTo(st.Width, .1f);
                wall.MoveTo(0, (int)(st.Height * .75f));

                expected = wall.Top;
                actual = m => m.Bounds.Bottom;
            }
            else
            {
                throw new NotSupportedException();
            }

            for (var speed = 5; speed < 1000; speed *= 2)
            {
                Console.WriteLine($"Speed: {speed}");
                var movingObject = st.Add(new SpacialElement(1, 1, movingObjectLocation.Left, movingObjectLocation.Top) { BackgroundColor = RGB.Blue });
                var v = new Velocity(movingObject);
                await st.DelayAsync(500);
                v.Angle = movementAngle;
                v.Speed = speed;
                await st.DelayAsync(20000);

                if(a == 90)
                {
                    Assert.IsTrue(movingObject.Bounds.Bottom < wall.Top);
                }
                else if(a == 270)
                {
                    Assert.IsTrue(movingObject.Top > wall.Bounds.Bottom);
                }
                else if(a == 180)
                {
                    Assert.IsTrue(movingObject.Left > wall.Bounds.Right);
                }
                else if(a == 0)
                {
                    Assert.IsTrue(movingObject.Bounds.Right < wall.Left);
                }
                else
                {
                    Assert.Fail();
                }

                Console.WriteLine($"Wall.Left == {expected}, movingObject.Right() == {actual(movingObject)}");
                movingObject.Lifetime.Dispose();
            }
        }
    }
}
