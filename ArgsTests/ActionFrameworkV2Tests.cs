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

    public class CommandsWithShortcuts
    {
        public static bool FooHappened = false;
        [ArgActionMethod, ArgShortcut("FooAlias")]
        public void Foo()
        {
            FooHappened = true;
        }
    }

    public class CommandsWithConflictingNames
    {
        [ArgActionMethod, ArgShortcut("Bar")]
        public void Foo()
        {

        }

        [ArgActionMethod]
        public void Bar()
        {

        }
    }


    [UsageAutomation]
    public class ActionScaffold
    {
        public bool GlobalFlag { get; set; }

        [ArgIgnore]
        public bool Command3Fired { get; private set; }
        [ArgIgnore]
        public bool Command4Fired { get; private set; }

        public static event Action Command5Fired;
        public static event Action HelpFired;

        [ArgIgnore]
        public string Command4FirstName { get; private set; }
        [ArgIgnore]
        public string Command4LastName { get; private set; }
        [ArgIgnore]
        public int Command4Age{ get; private set; }

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

        [ArgActionMethod]
        public void Command3()
        {
            Command3Fired = true;
        }

        [ArgActionMethod]
        public void Command4([ArgDescription("Your first name"), ArgRequired] string firstName, [ArgDescription("Your last name")]   string lastName, [ArgDescription("Your age")]   int age)
        {
            Command4FirstName = firstName;
            Command4LastName = lastName;
            Command4Age = age;
            Command4Fired = true;
        }

        

        [ArgActionMethod]
        public static void Command5()
        {
            if (Command5Fired != null) Command5Fired();
        }

        [ArgActionMethod, ArgShortcut("-?")]
        public static void Help()
        {
            if (HelpFired != null) HelpFired();
        }
    }

    [UsageAutomation]
    [ArgActionType(typeof(DeferredActions))]
    public class ActionScaffoldDeferred
    {
        public bool GlobalFlag { get; set; }
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

    [UsageAutomation]
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

    [UsageAutomation]
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

    public class ActionWithSharedPropertyNameArgs
    {
        [ArgIgnore]
        public bool DupeTriggered { get; set; }

        [ArgActionMethod]
        public void Duplicate(DuplicateArgsClass args)
        {
            if (args.Duplicate != "Dupe") Assert.Fail("'Dupe' value was not passed: "+args.Duplicate);
            DupeTriggered = true;
        }
    }

    public class DuplicateArgsClass
    {
        public string Duplicate { get; set; }
    }

    [TestClass]
    public class ActionFrameworkV2Tests
    {
        public static string Message { get; set; }

        [TestMethod]
        public void TestActionShortcuts()
        {
            CommandsWithShortcuts.FooHappened = false;
            Args.InvokeAction<CommandsWithShortcuts>("FooAlias");
            Assert.IsTrue(CommandsWithShortcuts.FooHappened);
        }

        [TestMethod]
        public void TestConflictingActionShortcuts()
        {
            try
            {
                Args.InvokeAction<CommandsWithConflictingNames>("Foo");
                Assert.Fail("An exception should have been thrown");
            }
            catch(InvalidArgDefinitionException)
            {

            }

            try
            {
                Args.InvokeAction<CommandsWithConflictingNames>("Bar");
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException)
            {

            }
        }


        [TestMethod]
        public void TestActionNameMatchesPropertyName()
        {
            var actionInfo = Args.InvokeAction<ActionWithSharedPropertyNameArgs>("Duplicate", "-Duplicate", "Dupe");
            Assert.IsInstanceOfType(actionInfo.ActionArgs, typeof(DuplicateArgsClass));
            Assert.AreEqual("Dupe", ((DuplicateArgsClass)actionInfo.ActionArgs).Duplicate);
            Assert.IsTrue(actionInfo.Args.DupeTriggered);
        }

        [TestMethod]
        public void TestParseOnlyActionsV2()
        {
            var actionInfo = Args.ParseAction<ActionScaffold>("Command1", "-s", "SomeStringValue", "-g");
            Assert.IsInstanceOfType(actionInfo.Args, typeof(ActionScaffold));
            Assert.IsInstanceOfType(actionInfo.ActionArgs, typeof(Command1Args));
        }

        [TestMethod]
        public void TestActionsWithNoParameters()
        {
            var actionInfo = Args.InvokeAction<ActionScaffold>("Command3");
            Assert.IsInstanceOfType(actionInfo.Args, typeof(ActionScaffold));
            Assert.IsTrue(actionInfo.Args.Command3Fired);
        }

        [TestMethod]
        public void TestStaticActions()
        {
            bool fired = false;

            Action handler = () => { fired = true; };
            ActionScaffold.Command5Fired += handler;
            try
            {
                var actionInfo = Args.InvokeAction<ActionScaffold>("Command5");
                Assert.IsInstanceOfType(actionInfo.Args, typeof(ActionScaffold));
                Assert.IsTrue(fired);
            }
            finally
            {
                ActionScaffold.Command5Fired -= handler;
            }
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
        public void TestInvokeActionsParamsAreArguments()
        {
            ActionFrameworkV2Tests.Message = null;

            var actionInfo = Args.InvokeAction<ActionScaffold>("Command4", "-firstName", "Adam", "-lastName", "Abdelhamed", "-age", "100");
            Assert.IsTrue(actionInfo.Args.Command4Fired);
            Assert.AreEqual("Adam",actionInfo.Args.Command4FirstName);
            Assert.AreEqual("Abdelhamed", actionInfo.Args.Command4LastName);
            Assert.AreEqual(100, actionInfo.Args.Command4Age);
            Assert.IsTrue(actionInfo.Definition.SpecifiedAction.Arguments[0].IsRequired);
            Assert.IsFalse(actionInfo.Definition.SpecifiedAction.Arguments[1].IsRequired);

            var usage = ArgUsage.GetUsage<ActionScaffold>("test");
            Assert.IsTrue(usage.Contains("Your first name"));
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
            Assert.AreEqual(null, actionInfo.Args.Command1Args);
            Assert.AreEqual("Command1", actionInfo.Args.Action);

            var actionInfo2 = Args.InvokeAction<ActionScaffoldWithActionPropertyAndAttributesAndProperties>("Command2", "-i", "1000", "-g");
            Assert.IsTrue(ActionFrameworkV2Tests.Message.Contains("1000"));
            Assert.AreEqual(null, actionInfo2.Args.Command2Args);
            Assert.AreEqual("Command2", actionInfo2.Args.Action);
        }

        [TestMethod]
        public void TestWithDeferredActionType()
        {
            ActionFrameworkV2Tests.Message = null;
            var actionInfo = Args.InvokeAction<ActionScaffoldDeferred>("Command1", "-s", "SomeStringValue", "-g");
            Assert.IsTrue(ActionFrameworkV2Tests.Message.Contains("SomeStringValue"));
        }

        [TestMethod]
        public void TestHelpWithAlias()
        {
            bool helpFired = false;
            Action helpHandler = () => { helpFired = true; };
            ActionScaffold.HelpFired += helpHandler;

            try
            {
                Args.InvokeAction<ActionScaffold>("-?");
                Assert.IsTrue(helpFired);
            }
            finally
            {
                ActionScaffold.HelpFired -= helpHandler;
            }
        }
    }
}
