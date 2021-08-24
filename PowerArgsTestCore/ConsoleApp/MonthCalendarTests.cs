using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;

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
            var app = new CliTestHarness(this.TestContext, 40, 20, true);
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
    }
}
