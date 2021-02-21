using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;

namespace ArgsTests.CLI.Games
{
    [TestClass]
    [TestCategory(Categories.Eventing)]
    public class RoutedEventTests
    {
        [TestMethod]
        public void TestRoutedEvents()
        {
            var routedEvent = new EventRouter<string>();

            var assertionCount = 0;

            var assertionIncrementCheck = new Action<Action>((a) =>
            {
                var origCount = assertionCount;
                a();
                Assert.AreEqual(origCount + 1, assertionCount);
            });

            routedEvent.RegisterOnce("Home/{Page}", (args) =>
            {
                Assert.AreEqual("thepage", args.RouteVariables["page"]);
                Assert.AreEqual(args.Data, "Foo");
                assertionCount++;
            });

            assertionIncrementCheck(() =>
            {
                routedEvent.Route("Home/ThePage", "Foo");
            });
            Console.WriteLine(assertionCount);
        }
        
    }
}
