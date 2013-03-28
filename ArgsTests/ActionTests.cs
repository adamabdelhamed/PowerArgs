using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class ActionTests
    {
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

        [ArgActionType(typeof(FakeProgram))]
        public class ActionTestArgsDeferredType
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            public SomeActionArgs SomeActionArgs { get; set; }
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
                Assert.IsTrue(ex.Message.ToLower().Contains("required") &&
                    ex.Message.ToLower().Contains("action"));
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
            catch (ArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("unknown") &&
                    ex.Message.ToLower().Contains("thisisnotarealaction"));
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
    }
}
