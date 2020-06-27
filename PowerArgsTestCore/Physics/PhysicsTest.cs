using ArgsTests.CLI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using System.Threading.Tasks;

namespace ArgsTests.CLI.Physics
{
    public static class PhysicsTest
    {
        public static readonly TimeSpan DefaultTimeIncrement = TimeSpan.FromSeconds(.05);
        public static async Task Test(int w, int h, TestContext testContext, Func<CliTestHarness, SpaceTimePanel,Task> test)
        {
            Exception stEx = null;
            var app = new CliTestHarness(testContext, w, h, true);
            app.SecondsBetweenKeyframes = DefaultTimeIncrement.TotalSeconds;
            app.InvokeNextCycle(async () =>
            {
                Deferred d = Deferred.Create();
                var spaceTimePanel = app.LayoutRoot.Add(new SpaceTimePanel(app.LayoutRoot.Width, app.LayoutRoot.Height));
                spaceTimePanel.SpaceTime.Increment = DefaultTimeIncrement;
                spaceTimePanel.SpaceTime.Start();
                spaceTimePanel.SpaceTime.UnhandledException.SubscribeForLifetime((ex) =>
                {
                    spaceTimePanel.SpaceTime.Stop();
                    d.Resolve();
                    ex.Handling = EventLoop.EventLoopExceptionHandling.Swallow;
                    stEx = ex.Exception;
                }, app);


                var justUpdated = false;
                spaceTimePanel.AfterUpdate.SubscribeForLifetime(() => justUpdated = true, app);

                app.AfterPaint.SubscribeForLifetime(() =>
                {
                    if (justUpdated)
                    {
                        app.RecordKeyFrame();
                        justUpdated = false;
                    }
                }, app);

                spaceTimePanel.SpaceTime.InvokeNextCycle(async () =>
                {
                    spaceTimePanel.RealTimeViewing.Enabled = false;
                    try
                    {
                        await test(app, spaceTimePanel);
                    }
                    catch (Exception ex)
                    {
                        stEx = ex;
                    }

                    await app.Paint().AsAwaitable();
                    d.Resolve();
                });
                await d.Promise.AsAwaitable();
                await spaceTimePanel.SpaceTime.YieldAsync();
                await app.PaintAndRecordKeyFrameAsync();
                spaceTimePanel.SpaceTime.Stop();

                await app.PaintAndRecordKeyFrameAsync();
                app.Stop();
            });

            await app.Start().AsAwaitable();
            if (stEx != null)
            {
                app.Abandon();
                Assert.Fail(stEx.ToString());
            }
            else
            {
                app.PromoteToLKG();
            }
        }

        public static void AssertClose(float expected, float actual, float maxDistance)
        {
            if(Math.Abs(expected-actual) > maxDistance)
            {
                Assert.Fail($"expected: {expected}, actual: {actual}, tolerance: {maxDistance}");
            }
        }
    }
}
