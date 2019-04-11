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
    public class GeometryTests
    {
        public TestContext TestContext { get; set; }

 

        [TestMethod]
        public async Task TestNormalizedProximity()
        {
            var app = new CliTestHarness(TestContext, 0, 0, 40, 40, true);
            app.QueueAction(async () =>
            {
                var a = app.LayoutRoot.Add(new ConsoleControl() { Background = ConsoleColor.Red, Width = 1, Height = 1, X = 0, Y = 0 });
                var b = app.LayoutRoot.Add(new ConsoleControl() { Background = ConsoleColor.Green, Width = 1, Height = 1, X = 39, Y = 39 });
                var d = Geometry.CalculateNormalizedDistanceTo(a, b);
                Console.WriteLine(d);
                await app.PaintAndRecordKeyFrameAsync();
                app.Stop();
            });

            await app.Start().AsAwaitable();
            app.AssertThisTestMatchesLKG();
        }
    }
}
