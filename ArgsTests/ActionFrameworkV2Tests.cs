using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    public class Command1Args
    {
        public string S1 { get; set; }
    }

    public class Command2Args
    {
        public int I1 { get; set; }
    }

    public class ActionScaffold
    {
        public bool GlobalFlag { get; set; }

        [ArgActionMethod]
        public void Command1(Command1Args commandArgs)
        {
            Assert.IsTrue(GlobalFlag);
            ActionFrameworkV2Tests.Message = "Command1: " + commandArgs.S1;
        }

        [ArgActionMethod]
        public void Command2(Command2Args commandArgs)
        {
            Assert.IsTrue(GlobalFlag);
            ActionFrameworkV2Tests.Message = "Command2: " + commandArgs.I1;
        }
    }

    [ArgActionType(typeof(DeferredActions))]
    public class ActionScaffoldDeferred
    {
        public bool GlobalFlag { get; set; }
    }

    public class ActionScaffoldWithActionPropertyAndAttributes
    {
        public bool GlobalFlag { get; set; }

        [ArgRequired]
        [ArgPosition(0)]
        public string Action { get; set; }

        [ArgActionMethod]
        public void Command1(Command1Args commandArgs)
        {
            Assert.IsTrue(GlobalFlag);
            ActionFrameworkV2Tests.Message = "Command1: " + commandArgs.S1;
        }

        [ArgActionMethod]
        public void Command2(Command2Args commandArgs)
        {
            Assert.IsTrue(GlobalFlag);
            ActionFrameworkV2Tests.Message = "Command2: " + commandArgs.I1;
        }
    }

    public class ActionScaffoldWithActionPropertyAndAttributesAndProperties
    {
        public bool GlobalFlag { get; set; }

        [ArgRequired]
        [ArgPosition(0)]
        public string Action { get; set; }

        [ArgIgnore]
        public Command1Args Command1Args { get; set; }

        [ArgIgnore]
        public Command2Args Command2Args { get; set; }


        [ArgActionMethod]
        public void Command1(Command1Args commandArgs)
        {
            Assert.IsTrue(GlobalFlag);
            ActionFrameworkV2Tests.Message = "Command1: " + commandArgs.S1;
        }

        [ArgActionMethod]
        public void Command2(Command2Args commandArgs)
        {
            Assert.IsTrue(GlobalFlag);
            ActionFrameworkV2Tests.Message = "Command2: " + commandArgs.I1;
        }
    }

    public class DeferredActions
    {
        [ArgActionMethod]
        public static void Command1(Command1Args commandArgs)
        {
            Assert.IsTrue(Args.GetAmbientArgs<ActionScaffoldDeferred>().GlobalFlag);
            ActionFrameworkV2Tests.Message = "Command1: " + commandArgs.S1;
        }

        [ArgActionMethod]
        public static void Command2(Command2Args commandArgs)
        {
            Assert.IsTrue(Args.GetAmbientArgs<ActionScaffoldDeferred>().GlobalFlag);
            ActionFrameworkV2Tests.Message = "Command2: " + commandArgs.I1;
        } 
    }

    [TestClass]
    public class ActionFrameworkV2Tests
    {
        public static string Message { get; set; }

        [TestMethod]
        public void TestParseOnlyActionsV2()
        {
            var actionInfo = Args.ParseAction<ActionScaffold>("Command1", "-s", "SomeStringValue", "-g");
            Assert.IsInstanceOfType(actionInfo.Args, typeof(ActionScaffold));
            Assert.IsInstanceOfType(actionInfo.ActionArgs, typeof(Command1Args));
        }

        [TestMethod]
        public void TestInvokeActionsV2()
        {
            ActionFrameworkV2Tests.Message = null;

            var actionInfo = Args.InvokeAction<ActionScaffold>("Command1", "-s", "SomeStringValue", "-g");
            Assert.IsTrue(ActionFrameworkV2Tests.Message.Contains("SomeStringValue"));

            var actionInfo2 = Args.InvokeAction<ActionScaffold>("Command2", "-i", "1000", "-g");
            Assert.IsTrue(ActionFrameworkV2Tests.Message.Contains("1000"));
        }

        [TestMethod]
        public void TestInvokeActionsV2Mixed()
        {
            ActionFrameworkV2Tests.Message = null;

            var actionInfo = Args.InvokeAction<ActionScaffoldWithActionPropertyAndAttributes>("Command1", "-s", "SomeStringValue", "-g");
            Assert.IsTrue(ActionFrameworkV2Tests.Message.Contains("SomeStringValue"));
            Assert.AreEqual("Command1", actionInfo.Args.Action);

            var actionInfo2 = Args.InvokeAction<ActionScaffoldWithActionPropertyAndAttributes>("Command2", "-i", "1000", "-g");
            Assert.IsTrue(ActionFrameworkV2Tests.Message.Contains("1000"));
            Assert.AreEqual("Command2", actionInfo2.Args.Action);
        }

        [TestMethod]
        public void TestInvokeActionsV2Mixed2()
        {
            ActionFrameworkV2Tests.Message = null;

            var actionInfo = Args.InvokeAction<ActionScaffoldWithActionPropertyAndAttributesAndProperties>("Command1", "-s", "SomeStringValue", "-g");
            Assert.IsTrue(ActionFrameworkV2Tests.Message.Contains("SomeStringValue"));
            Assert.AreEqual("SomeStringValue", actionInfo.Args.Command1Args.S1);
            Assert.AreEqual("Command1", actionInfo.Args.Action);

            var actionInfo2 = Args.InvokeAction<ActionScaffoldWithActionPropertyAndAttributesAndProperties>("Command2", "-i", "1000", "-g");
            Assert.IsTrue(ActionFrameworkV2Tests.Message.Contains("1000"));
            Assert.AreEqual(1000, actionInfo2.Args.Command2Args.I1);
            Assert.AreEqual("Command2", actionInfo2.Args.Action);
        }

        [TestMethod]
        public void TestWithDeferredActionType()
        {
            ActionFrameworkV2Tests.Message = null;
            var actionInfo = Args.InvokeAction<ActionScaffoldDeferred>("Command1", "-s", "SomeStringValue", "-g");
            Assert.IsTrue(ActionFrameworkV2Tests.Message.Contains("SomeStringValue"));
        }
    }
}
