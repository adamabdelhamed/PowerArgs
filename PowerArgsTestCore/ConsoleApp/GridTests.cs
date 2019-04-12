using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace ArgsTests.CLI
{
    [TestClass]
    [TestCategory(Categories.ConsoleApp)]
    public class GridTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
       public void TestBasicGrid()
       {
            var grid = new Grid(new List<object>
            {
                new { Name = "Adam", State = "Washington" },
                new { Name = "Bob", State = "New Jersey" }
            });

            var app = new CliTestHarness(this.TestContext, 80,30);
            app.LayoutRoot.Add(grid).Fill();
            app.QueueAction(() =>
            {
                app.SetTimeout(() => app.SendKey(new ConsoleKeyInfo((char)0, ConsoleKey.DownArrow, false,false,false)), TimeSpan.FromMilliseconds(333));
                app.SetTimeout(() => app.SendKey(new ConsoleKeyInfo((char)0, ConsoleKey.UpArrow, false, false, false)), TimeSpan.FromMilliseconds(666));
                app.SetTimeout(() => app.Stop(), TimeSpan.FromMilliseconds(1000));
            });
            app.Start().Wait();

            app.AssertThisTestMatchesLKG();
        }
    }
}
