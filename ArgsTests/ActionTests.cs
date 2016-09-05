using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Threading.Tasks;
using System.Threading;

namespace ArgsTests
{
    [TestClass]
    public class ActionTests
    {
        [UsageAutomation]
        public class ActionTestArgs
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            public SomeActionArgs SomeActionArgs { get; set; }


            public static int InvokeCount { get; set; }
            public static void SomeAction(SomeActionArgs args)
            {
                InvokeCount++;
            }
        }

        [UsageAutomation]
        public class AsyncActionTestArgs
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            public SomeActionArgs SomeActionArgs { get; set; }


            public static int InvokeCount { get; set; }
            public static Task SomeAction(SomeActionArgs args)
            {
                return Task.Factory.StartNew(() => 
                {
                    InvokeCount++;
                });
            }
        }

        [UsageAutomation]
        public class AsyncMainTestArgs
        {
            public static int InvokeCount { get; set; }
            public Task Main()
            {
                return Task.Factory.StartNew(() =>
                {
                    InvokeCount++;
                });
            }
        }

        [UsageAutomation]
        public class AsyncActionTestArgsNonStatic
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            public SomeActionArgs SomeActionArgs { get; set; }


            public static int InvokeCount { get; set; }
            public Task SomeAction(SomeActionArgs args)
            {
                return Task.Factory.StartNew(() =>
                {
                    InvokeCount++;
                });
            }
        }

        [UsageAutomation]
        [ArgActionType(typeof(FakeProgram))]
        public class ActionTestArgsDeferredType
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            public SomeActionArgs SomeActionArgs { get; set; }
        }

        public class ActionTestArgsActionMethodHasNoParemeters
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            public SomeActionArgs SomeActionArgs { get; set; }

            public void SomeAction()
            {
                Assert.Fail("This should never get called");
            }
        }


        public class ActionTestArgsActionMethodHasWrongParameterType
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            public SomeActionArgs SomeActionArgs { get; set; }

            public void SomeAction(object o)
            {
                Assert.Fail("This should never get called");
            }
        }

        public class ActionTestArgsActionMethodHasTooManyParameters
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            [ArgActionMethod]
            public void SomeAction(SomeActionArgs o, bool someOtherParemeter)
            {
                Assert.Fail("This should never get called");
            }
        }

        public class FakeProgram
        {
            public static int InvokeCount { get; set; }
            public static void SomeAction(SomeActionArgs args)
            {
                InvokeCount++;
            }
        }

        public class InvalidActionArgs
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            public SomeActionArgs SomeActionArgs { get; set; }
            
            // Missing the action method impl
            // public static void SomeAction(SomeActionArgs args){}
        }

        public class SomeActionArgs
        {
            [ArgPosition(1)]
            public string A { get; set; }

            [ArgPosition(2)]
            public string B { get; set; }
        }

        public class FailingProgram
        {
            [ArgActionMethod]
            public static void SomeAction()
            {
                throw new Exception();
            }
        }

        [TestMethod]
        public void TestBasicActionBinding()
        {
            var args = new string[] { "someaction", "aval", "bval" };
            var beforeCount = ActionTestArgs.InvokeCount;
            var parsed = Args.InvokeAction<ActionTestArgs>(args);
            Assert.AreEqual("aval", parsed.Args.SomeActionArgs.A);
            Assert.AreEqual("bval", parsed.Args.SomeActionArgs.B);
            Assert.AreEqual(beforeCount + 1, ActionTestArgs.InvokeCount);
        }

        [TestMethod]
        public void TestBasicActionBindingAsync()
        {
            var args = new string[] { "someaction", "aval", "bval" };
            var beforeCount = AsyncActionTestArgs.InvokeCount;
            var parsed = Args.InvokeAction<AsyncActionTestArgs>(args);
            Assert.AreEqual("aval", parsed.Args.SomeActionArgs.A);
            Assert.AreEqual("bval", parsed.Args.SomeActionArgs.B);
            Assert.AreEqual(beforeCount + 1, AsyncActionTestArgs.InvokeCount);
        }


        [TestMethod]
        public async Task TestBasicActionBindingE2EAsync()
        {
            var args = new string[] { "someaction", "aval", "bval" };
            var beforeCount = AsyncActionTestArgs.InvokeCount;
            var parsed = await Args.InvokeActionAsync<AsyncActionTestArgs>(args);
            Assert.AreEqual("aval", parsed.Args.SomeActionArgs.A);
            Assert.AreEqual("bval", parsed.Args.SomeActionArgs.B);
            Assert.AreEqual(beforeCount + 1, AsyncActionTestArgs.InvokeCount);
        }

        [TestMethod]
        public void TestAsyncMain()
        {
            var args = new string[] {  };
            var beforeCount = AsyncMainTestArgs.InvokeCount;
            var parsed = Args.InvokeMain<AsyncMainTestArgs>(args);
            Assert.AreEqual(beforeCount + 1, AsyncMainTestArgs.InvokeCount);
        }

        [TestMethod]
        public async Task TestAsyncMainE2E()
        {
            var args = new string[] { };
            var beforeCount = AsyncMainTestArgs.InvokeCount;
            var parsed = await Args.InvokeMainAsync<AsyncMainTestArgs>(args);
            Assert.AreEqual(beforeCount + 1, AsyncMainTestArgs.InvokeCount);
        }

        [TestMethod]
        public void TestBasicActionBindingAsyncNonStatic()
        {
            var args = new string[] { "someaction", "aval", "bval" };
            var beforeCount = AsyncActionTestArgsNonStatic.InvokeCount;
            var parsed = Args.InvokeAction<AsyncActionTestArgsNonStatic>(args);
            Assert.AreEqual("aval", parsed.Args.SomeActionArgs.A);
            Assert.AreEqual("bval", parsed.Args.SomeActionArgs.B);
            Assert.AreEqual(beforeCount + 1, AsyncActionTestArgsNonStatic.InvokeCount);
        }

        [TestMethod]
        public void TestDeferredActionBinding()
        {
            var args = new string[] { "someaction", "aval", "bval" };
            var beforeCount = FakeProgram.InvokeCount;
            var parsed = Args.InvokeAction<ActionTestArgsDeferredType>(args);
            Assert.AreEqual("aval", parsed.Args.SomeActionArgs.A);
            Assert.AreEqual("bval", parsed.Args.SomeActionArgs.B);
            Assert.AreEqual(beforeCount + 1, FakeProgram.InvokeCount);
        }

        [TestMethod]
        public void TestUnspecifiedAction()
        {
            var args = new string[] {  };
            try
            {
                var parsed = Args.InvokeAction<ActionTestArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("no action was specified"));
            }
        }

        [TestMethod]
        public void TestUnknownAction()
        {
            var args = new string[] { "thisisnotarealaction" };
            try
            {
                var parsed = Args.InvokeAction<ActionTestArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (UnknownActionArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("thisisnotarealaction"));
            }
        }

        [TestMethod]
        public void TestMissingActionBinding()
        {
            var args = new string[] { "someaction", "aval", "bval" };

            try
            {
                var parsed = Args.ParseAction<InvalidActionArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {

            }
        }

        [TestMethod]
        public void TestActionHasNoParameters()
        {
            var args = new string[] { "someaction", "aval", "bval" };

            try
            {
                var parsed = Args.ParseAction<ActionTestArgsActionMethodHasNoParemeters>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.AreEqual("PowerArg action methods must take one parameter that matches the property type for the attribute", ex.Message);
            }
        }

        [TestMethod]
        public void TestActionHasWrongParameterType()
        {
            var args = new string[] { "someaction", "aval", "bval" };

            try
            {
                var parsed = Args.ParseAction<ActionTestArgsActionMethodHasWrongParameterType>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.AreEqual("Argument of type 'Object' does not match expected type 'SomeActionArgs'", ex.Message);
            }
        }

        [TestMethod]
        public void TestActionHasTooManyParameters()
        {
            var args = new string[] { "someaction", "aval", "bval" };

            try
            {
                var parsed = Args.ParseAction<ActionTestArgsActionMethodHasTooManyParameters>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.AreEqual("Your action method contains a parameter that cannot be revived on its own.  That is only valid if the non-revivable parameter is the only parameter.  In that case, the properties of that parameter type will be used.", ex.Message);
            }
        }

        [TestMethod]
        [Description("NOTE: This tests a highly unlikely case as normal usage will have the Args class throw an exception first.")]
        public void ArgActionInvokeThrowsOnNullAction()
        {
            var args = new string[] { };
            var argAction = new ArgAction<ActionTestArgs>();
            
            // Not setting .ActionArgs or .Args to leave them as null.

            try
            {
                argAction.Invoke();
                Assert.Fail("Should have thrown an exception.");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(MissingArgException));
                Assert.AreEqual("No action was specified", ex.Message);
            }
        }

        [TestMethod]
        public void ActionExceptionPreservesOriginalStackTrace()
        {
            try
            {
                var args = new string[] { "SomeAction" };
                Args.InvokeAction<FailingProgram>(args);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.StackTrace.Contains("SomeAction"), "Stack trace did not contain original stack trace");
            }
        }
    }
}
