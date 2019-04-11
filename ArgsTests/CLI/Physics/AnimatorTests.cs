using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ArgsTests.CLI.Physics
{

    public class KeyframeDelayProvider : IDelayProvider
    {
        private CliTestHarness app;
        public KeyframeDelayProvider(CliTestHarness app)
        {
            this.app = app;
        }

        public async Task DelayAsync(double ms)
        {
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.DelayAsync(ms);
        }

        public async Task DelayAsync(TimeSpan timeout)
        {
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.DelayAsync(timeout);
        }

        public async Task DelayAsync(Event ev, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.DelayAsync(ev, timeout, evalFrequency);
        }

        public async Task DelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.DelayAsync(condition, timeout, evalFrequency);
        }

        public async Task<bool> TryDelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            await app.PaintAndRecordKeyFrameAsync();
            return await Time.CurrentTime.TryDelayAsync(condition, timeout, evalFrequency);
        }

        public async Task YieldAsync()
        {
            await app.PaintAndRecordKeyFrameAsync();
            await Time.CurrentTime.YieldAsync();
        }
    }

    [TestClass]
    public class AnimatorTests
    {
        public TestContext TestContext { get; set; }

 

        [TestMethod]
        public async Task TestAnimatorInTimeAsync()
        {
            var app = new CliTestHarness(TestContext, 0, 0, 40, 1, true);
            app.QueueAction(() =>
            {
                var panel = app.LayoutRoot.Add(new SpacetimePanel(40, 1));
        
                panel.SpaceTime.Start();
                
                app.SecondsBetweenKeyframes = panel.SpaceTime.Increment.TotalSeconds;
                panel.SpaceTime.QueueAction(async ()=>
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
                        DelayProvider = new KeyframeDelayProvider(app),
                        AutoReverse = true,
                        AutoReverseDelay = 1000,
                    });
    
                    app.Stop();
                });
            });

            await app.Start().AsAwaitable();
            app.AssertThisTestMatchesLKG();
        }
    }
}
