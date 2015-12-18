using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using System.Collections.Generic;
using System.ComponentModel;

namespace ArgsTests.Pages
{
    [TestClass]
    public class PageStackTests
    {
        [TestMethod]
        public void PageStackBasic()
        {
            PageStack stack = new PageStack();

            bool observableWorked = false;

            PropertyChangedEventHandler firstChecker = (sender, e) =>
            {
                if(e.PropertyName != nameof(PageStack.CurrentPage))
                {
                    return;
                }

                Assert.IsNotNull(stack.CurrentPage);
                Assert.AreEqual(0, stack.CurrentPage.RouteVariables.Count);
                observableWorked = true;
            };

            stack.PropertyChanged += firstChecker;
            stack.RegisterRoute("Home", () => new Page());
            stack.Navigate("Home");
            Assert.IsTrue(observableWorked);
            stack.PropertyChanged -= firstChecker;

            try
            {
                stack.Navigate("BadRoute");
                Assert.Fail("An exception should have been thrown");
            }
            catch (KeyNotFoundException)
            {

            }

            stack.RegisterRoute("Applications/{ApplicationId}/Components/{ComponentId}", () => new Page());

            stack.Navigate("Applications/foo/Components/bar");
            Assert.IsTrue(stack.CurrentPage.RouteVariables.Count == 2);
            Assert.AreEqual("foo", stack.CurrentPage.RouteVariables["ApplicationId"]);
            Assert.AreEqual("bar", stack.CurrentPage.RouteVariables["ComponentId"]);
        }

        [TestMethod]
        public void PageStackDefault()
        {
            PageStack stack = new PageStack();

            stack.RegisterDefaultRoute("home", () => new Page());
            Assert.IsNull(stack.CurrentPage);
            stack.Navigate("");
            Assert.IsNotNull(stack.CurrentPage);
        }

        [TestMethod]
        public void PageStackRoutesEdgeCases()
        {
            ExpectBadRoute("");
            ExpectBadRoute("A ");
            ExpectBadRoute("@#@$#%#$");
            ExpectBadRoute("a//b");
            ExpectBadRoute("a/b//");
            ExpectBadRoute("*");

            ExpectGoodRoute("a");
            ExpectGoodRoute("a/b");
            ExpectGoodRoute("a/b/");
            ExpectGoodRoute("a/b/c");
            ExpectGoodRoute("a/b/c/");
        }

        private void ExpectBadRoute(string badRoute)
        {
            PageStack stack = new PageStack();
            try
            {
                stack.RegisterRoute(badRoute, () => new Page());
                Assert.Fail("An exception should have been thrown for bad route: "+ badRoute);
            }
            catch (FormatException) { }
        }

        private void ExpectGoodRoute(string goodRoute)
        {
            PageStack stack = new PageStack();
            stack.RegisterRoute(goodRoute, () => new Page());
        }
    }
}
