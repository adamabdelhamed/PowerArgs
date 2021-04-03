using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ArgsTests.CLI
{

    public class KeyframeDelayProvider : IDelayProvider
    {
        private CliTestHarness app;
        public int DelayCount = 0;
        public KeyframeDelayProvider(CliTestHarness app)
        {
            this.app = app;
        }

        public async Task DelayAsync(double ms)
        {
            DelayCount++;
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.DelayAsync(ms);
        }

        public async Task DelayAsync(TimeSpan timeout)
        {
            DelayCount++;
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.DelayAsync(timeout);
        }

        public async Task DelayAsync(Event ev, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            DelayCount++;
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.DelayAsync(ev, timeout, evalFrequency);
        }

        public async Task DelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            DelayCount++;
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.DelayAsync(condition, timeout, evalFrequency);
        }

        public async Task<bool> TryDelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            DelayCount++;
            await app.PaintAndRecordKeyFrameAsync();
            return await Time.CurrentTime.TryDelayAsync(condition, timeout, evalFrequency);
        }
   
        public async Task YieldAsync()
        {
            DelayCount++;
            await app.PaintAndRecordKeyFrameAsync();
            await Task.Yield();
        }

        public async Task DelayFuzzyAsync(float ms, double maxDeltaPercentage = 0.1)
        {
            DelayCount++;
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.DelayFuzzyAsync(ms, maxDeltaPercentage);
        }

        public void DelayThen(float delay, Action then)
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    [TestCategory(Categories.Physics)]
    public class AnimatorTests
    {
        public TestContext TestContext { get; set; }

 

        [TestMethod]
        public async Task TestAnimatorInTimeAsync()
        {
            var app = new CliTestHarness(TestContext, 40, 1, true);
            var delayProvider = new KeyframeDelayProvider(app);
            app.InvokeNextCycle(() =>
            {
                var panel = app.LayoutRoot.Add(new SpaceTimePanel(40, 1));
                panel.SpaceTime.Start();
                app.SecondsBetweenKeyframes = panel.SpaceTime.Increment.TotalSeconds;
                panel.SpaceTime.Invoke(async ()=>
                {
                    panel.RealTimeViewing.Enabled = false;
                    var element = panel.SpaceTime.Add(new SpacialElement());
                    element.ResizeTo(1, 1);
                    await app.PaintAndRecordKeyFrameAsync();
            
                    await Animator.AnimateAsync(new FloatAnimatorOptions()
                    {
                        From = 0,
                        To = panel.Width-1,
                        Duration = 3000,
                        Setter = v => element.MoveTo(v, element.Top),
                        DelayProvider =delayProvider,
                        AutoReverse = true,
                        AutoReverseDelay = 1000,
                    });
                    panel.SpaceTime.Stop();
                    app.Stop();
                });
            });

            await app.Start();
            app.AssertThisTestMatchesLKG();
            Console.WriteLine(delayProvider.DelayCount+" delays");
            Console.WriteLine(app.TotalPaints + " paints");
            Console.WriteLine(app.TotalCycles + " cycles");
        }
    }
}
