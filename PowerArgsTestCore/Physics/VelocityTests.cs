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
            await TestCantGoThroughWalls(Direction.Right, app, stPanel);
        });

        [TestMethod]
        public async Task TestCantGoThroughWallsLeft() => await PhysicsTest.Test(50, 25, TestContext, async (app, stPanel) =>
        {
            await TestCantGoThroughWalls(Direction.Left, app, stPanel);
        });

        [TestMethod]
        public async Task TestCantGoThroughWallsUp() => await PhysicsTest.Test(50, 25, TestContext, async (app, stPanel) =>
        {
            await TestCantGoThroughWalls(Direction.Up, app, stPanel);
        });

        [TestMethod]
        public async Task TestCantGoThroughWallsDown() => await PhysicsTest.Test(50, 25, TestContext, async (app, stPanel) =>
        {
            await TestCantGoThroughWalls(Direction.Down, app, stPanel);
        });

        private async Task TestCantGoThroughWalls(Direction d, CliTestHarness app, SpaceTimePanel stPanel)
        {
            var st = stPanel.SpaceTime;
            var wall = st.Add(new SpacialElement() { BackgroundColor = RGB.DarkRed });
            ILocationF movingObjectLocation;
            float movementAngle;
            float expected;
            Func<SpacialElement,float> actual;
            if(d == Direction.Right)
            {
                movingObjectLocation = LocationF.Create((int)(st.Width * .25f), st.Height * .5f - .5f);
                movementAngle = 0;
                wall.ResizeTo(.1f, st.Height);
                wall.MoveTo((int)(st.Width * .75f), 0);

                expected = wall.Left;
                actual = m => m.Right();
            }
            else if(d == Direction.Left)
            {
                movingObjectLocation = LocationF.Create((int)(st.Width * .75f), st.Height * .5f - .5f);
                movementAngle = 180;
                wall.ResizeTo(.1f, st.Height);
                wall.MoveTo((int)(st.Width * .25f)-.1f, 0);

                expected = wall.Right();
                actual = m => m.Left;
            }
            else if (d == Direction.Up)
            {
                movingObjectLocation = LocationF.Create(st.Width * .5f - .5f, (int)(st.Height * .75f));
                movementAngle = 270;
                wall.ResizeTo(st.Width, .1f);
                wall.MoveTo(0, (int)(st.Height * .25f)-.1f);

                expected = wall.Bottom();
                actual = m => m.Top;
            }
            else if (d == Direction.Down)
            {
                movingObjectLocation = LocationF.Create(st.Width * .5f - .5f, (int)(st.Height * .25f));
                movementAngle = 90;
                wall.ResizeTo(st.Width, .1f);
                wall.MoveTo(0, (int)(st.Height * .75f));

                expected = wall.Top;
                actual = m => m.Bottom();
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

                if(d == Direction.Down)
                {
                    Assert.IsTrue(movingObject.Bottom() < wall.Top);
                }
                else if(d == Direction.Up)
                {
                    Assert.IsTrue(movingObject.Top > wall.Bottom());
                }
                else if(d == Direction.Left)
                {
                    Assert.IsTrue(movingObject.Left > wall.Right());
                }
                else if(d == Direction.Right)
                {
                    Assert.IsTrue(movingObject.Right() < wall.Left);
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
