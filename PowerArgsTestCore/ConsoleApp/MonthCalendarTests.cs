using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using System;
using System.Threading.Tasks;

namespace ArgsTests.CLI.Controls
{
    [TestClass]
    [TestCategory(Categories.ConsoleApp)]
    public class MonthCalendarTests
    {
        public TestContext TestContext { get; set; }
        
        [TestMethod]
        public void TestMonthCalendarBasicRender()
        {
            var app = new CliTestHarness(this.TestContext, MonthCalendar.MinWidth, MonthCalendar.MinHeight, true);
            app.InvokeNextCycle(() => app.LayoutRoot.Add(new MonthCalendar(new MonthCalendarOptions() { Year = 2000, Month = 1 })).Fill());
            app.InvokeNextCycle(async () =>
            {
                await app.Paint();
                app.RecordKeyFrame();
                app.Stop();
            });
            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }

        [TestMethod]
        public void TestMonthCalendarFocusAndNav()
        {
            var app = new CliTestHarness(this.TestContext, MonthCalendar.MinWidth, MonthCalendar.MinHeight, true);
            app.InvokeNextCycle(async () =>
            {
                var calendar = app.LayoutRoot.Add(new MonthCalendar(new MonthCalendarOptions() { Year = 2000, Month = 1 })).Fill();
                await app.PaintAndRecordKeyFrameAsync();
                calendar.TryFocus();
                await app.PaintAndRecordKeyFrameAsync();
                var fwInfo = new ConsoleKeyInfo('a', calendar.Options.AdvanceMonthForwardKey.Key, false, false, false);
                var backInfo = new ConsoleKeyInfo('b', calendar.Options.AdvanceMonthBackwardKey.Key, false, false, false);
                await app.SendKey(backInfo);
                await app.PaintAndRecordKeyFrameAsync();
                await app.SendKey(fwInfo);
                await app.PaintAndRecordKeyFrameAsync();
                app.Stop();
            });
            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }

        [TestMethod]
        public void TestThreeMonthCalendarBasicRender()
        {
            var app = new CliTestHarness(this.TestContext, 120, 40);
            app.InvokeNextCycle(async () =>
            {
                var carousel = new ThreeMonthCarousel(new ThreeMonthCarouselOptions());
                var start = carousel.Options.Month + "/" + carousel.Options.Year;
                await Task.Delay(1000);
                app.LayoutRoot.Add(new FixedAspectRatioPanel(4f / 1f, carousel)).Fill();
                Assert.IsTrue(await carousel.SeekAsync(true, carousel.Options.AnimationDuration));
                await Task.Delay(1000);
                Assert.IsTrue(await carousel.SeekAsync(true, carousel.Options.AnimationDuration));
                await Task.Delay(1000);
                Assert.IsTrue(await carousel.SeekAsync(true, carousel.Options.AnimationDuration));
                await Task.Delay(1000);
                Assert.IsTrue(await carousel.SeekAsync(true, carousel.Options.AnimationDuration));
   
                await Task.Delay(3000);

                var now = carousel.Options.Month + "/" + carousel.Options.Year;
                Assert.AreNotEqual(start, now);

                Assert.IsTrue(await carousel.SeekAsync(false, carousel.Options.AnimationDuration));
                await Task.Delay(1000);
                Assert.IsTrue(await carousel.SeekAsync(false, carousel.Options.AnimationDuration));
                await Task.Delay(1000);
                Assert.IsTrue(await carousel.SeekAsync(false, carousel.Options.AnimationDuration));
                await Task.Delay(1000);
                Assert.IsTrue(await carousel.SeekAsync(false, carousel.Options.AnimationDuration));
                await Task.Delay(1000);

                now = carousel.Options.Month + "/" + carousel.Options.Year;
                Assert.AreEqual(start, now);
                app.Stop();
            });
            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }
    }
}
