using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

using System.Linq;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class ModelTests
    {
        [TestMethod]
        public void TestEmptyModel()
        {
            CommandLineArgumentsDefinition definition = new CommandLineArgumentsDefinition();
            Args.Parse(definition, new string[0]);
        }

        [TestMethod]
        public void TestSimpleModel()
        {
            CommandLineArgumentsDefinition definition = new CommandLineArgumentsDefinition();
            var argument = new CommandLineArgument(typeof(int), "somenumber");
            definition.Arguments.Add(argument);

            var argumentString = argument.ToString(); // Make sure it doesn't throw

            Args.Parse(definition, new string[] { "-somenumber", "100" });
            Assert.AreEqual(100, definition.Arguments[0].RevivedValue);
            
            definition.Arguments[0].RevivedValue = null;
            Args.Parse(definition, new string[] { "/somenumber:100" });
            Assert.AreEqual(100, definition.Arguments[0].RevivedValue);
        }

        [TestMethod]
        public void TestSimpleModelWithSimpleValidator()
        {
            CommandLineArgumentsDefinition definition = new CommandLineArgumentsDefinition();
            var definitionString = definition.ToString(); // Make sure it doesn't throw


            var argument = new CommandLineArgument(typeof(int), "somenumber");
            definition.Arguments.Add(argument);
            argument.Metadata.Add(new ArgRequired());

            try
            {
                Args.Parse(definition, new string[] { });
                Assert.Fail("An exception should have been thrown");
            }
            catch (MissingArgException ex)
            {
                Assert.IsTrue(ex.Message.Contains("somenumber"));
            }
        }

        [TestMethod]
        public void TestModelDrivenUsage()
        {
            CommandLineArgumentsDefinition definition = new CommandLineArgumentsDefinition();
            var argument1 = new CommandLineArgument(typeof(int), "somenumber");
            argument1.Description = "INT Description";

            var argument2 = new CommandLineArgument(typeof(Uri), "someuri");
            argument2.Description = "URI Description";

            definition.Arguments.Add(argument1);
            definition.Arguments.Add(argument2);

            var usage = ArgUsage.GenerateUsageFromTemplate(definition);

            Assert.IsTrue(usage.Contains("INT Description"));
            Assert.IsTrue(usage.Contains("URI Description"));
        }

        [TestMethod]
        public void TestModeledAction()
        {
            bool invoked = false;

            CommandLineArgumentsDefinition definition = new CommandLineArgumentsDefinition();

            var action = new CommandLineAction((d) =>
            {
                Assert.AreEqual("go", d.SpecifiedAction.DefaultAlias);
                Assert.AreEqual("Hawaii", d.SpecifiedAction.Arguments[0].RevivedValue);
                invoked = true;
            });
            
            action.Aliases.Add("go");
            action.Description = "A simple action";

            definition.Actions.Add(action);

            var actionString = action.ToString(); // Make sure it doesn't throw

            var destination  = new CommandLineArgument(typeof(string),"destination");
            destination.Metadata.Add(new ArgRequired());
            destination.Description = "The place to go to";

            action.Arguments.Add(destination);

            Args.InvokeAction(definition, "go", "-destination", "Hawaii");
            Assert.IsTrue(invoked);

            var usage = ArgUsage.GenerateUsageFromTemplate(definition);
            Assert.IsTrue(usage.Contains("A simple action"));
            Assert.IsTrue(usage.Contains("The place to go to"));
        }

        [TestMethod]
        public void TestTheTestIsValidAndRevivableMethod()
        {
            var arg = new CommandLineArgument(typeof(int), "TheInt");
            Assert.IsTrue(arg.TestIsValidAndRevivable("100"));

            // this should fail on the revive test
            Assert.IsFalse(arg.TestIsValidAndRevivable("abc"));

            Assert.IsTrue(arg.TestIsValidAndRevivable("2000"));
            arg.Metadata.Add(new ArgRegex("1000"));

            // this should fail the validation test
            Assert.IsFalse(arg.TestIsValidAndRevivable("2000"));
        }

        [TestMethod]
        public void TestPowerArgsRichCommandLineReaderFindContextualArgument()
        {
            var args = new PowerArgsRichCommandLineReader.FindContextualArgumentArgs()
            {
                CurrentTokenIndex = 0,
                CommandLine = "-TheString",
                PreviousToken = "-TheString",
            };
            try
            {
                PowerArgsRichCommandLineReader.FindContextualArgument(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch(NullReferenceException ex)
            {
                Assert.IsTrue(ex.Message.Contains("ambient"));
            }

            CommandLineArgumentsDefinition def = new CommandLineArgumentsDefinition();
            var globalArg = new CommandLineArgument(typeof(string), "TheString");

            def.Arguments.Add(globalArg);

            args.Definition = def;
            var found = PowerArgsRichCommandLineReader.FindContextualArgument(args);
            Assert.AreSame(globalArg, found);

            args.CommandLine = "/TheString";
            args.PreviousToken = args.CommandLine;
            found = PowerArgsRichCommandLineReader.FindContextualArgument(args);
            Assert.AreSame(globalArg, found);

            args.CommandLine = "-ActionInt";
            args.PreviousToken = args.CommandLine;
            Assert.IsNull(PowerArgsRichCommandLineReader.FindContextualArgument(args));
            Assert.IsNull(PowerArgsRichCommandLineReader.FindContextualArgument(args));

            var action = new CommandLineAction((d) => { });
            action.Aliases.Add("TheAction");

            var actionArg = new CommandLineArgument(typeof(int), "ActionInt");
            action.Arguments.Add(actionArg);
            def.Actions.Add(action);

            args.CommandLine = "-TheString";
            args.PreviousToken = args.CommandLine;
            args.ActionContext = action;
            found = PowerArgsRichCommandLineReader.FindContextualArgument(args);
            Assert.AreSame(globalArg, found);

            args.CommandLine = "-ActionInt";
            args.PreviousToken = args.CommandLine;
            found = PowerArgsRichCommandLineReader.FindContextualArgument(args);
            Assert.AreSame(actionArg, found);
        }

        [TestMethod]
        public void TestPowerArgsRichCommandLineReaderFindContextualAction()
        {
            try
            {
                PowerArgsRichCommandLineReader.FindContextualAction("doesnotmatter");
                Assert.Fail("An exception should have been thrown");
            }
            catch (NullReferenceException ex)
            {
                Assert.IsTrue(ex.Message.Contains("ambient"));
            }

            CommandLineArgumentsDefinition def = new CommandLineArgumentsDefinition();
            Assert.IsNull(PowerArgsRichCommandLineReader.FindContextualAction(null, def));
            Assert.IsNull(PowerArgsRichCommandLineReader.FindContextualAction("", def));
            Assert.IsNull(PowerArgsRichCommandLineReader.FindContextualAction("NonMatchingAction", def));

            var action = new CommandLineAction((d) => { });
            action.Aliases.Add("TheAction");

            def.Actions.Add(action);

            var found = PowerArgsRichCommandLineReader.FindContextualAction("theaction", def);
            Assert.AreSame(action, found);
        }

        [TestMethod]
        public void TestPowerArgsRichCommandLineReaderFindCurrentTokenArgument()
        {
            bool expect;
            try
            {
                PowerArgsRichCommandLineReader.FindCurrentTokenArgument(null, null, out expect);
            }
            catch (NullReferenceException ex)
            {
                Assert.IsTrue(ex.Message.Contains("ambient"));
            }

            CommandLineArgumentsDefinition def = new CommandLineArgumentsDefinition();
            var globalArg = new CommandLineArgument(typeof(int), "TheInt");
            def.Arguments.Add(globalArg);
            Assert.IsNull(PowerArgsRichCommandLineReader.FindCurrentTokenArgument(null, null, out expect, def));
            Assert.IsFalse(expect);

            var found = PowerArgsRichCommandLineReader.FindCurrentTokenArgument(null, "-TheInt", out expect, def);
            Assert.AreSame(globalArg, found);
            Assert.IsTrue(expect);

            found = PowerArgsRichCommandLineReader.FindCurrentTokenArgument(null, "/TheInt", out expect, def);
            Assert.AreSame(globalArg, found);
            Assert.IsTrue(expect);

            found = PowerArgsRichCommandLineReader.FindCurrentTokenArgument(null, "TheInt", out expect, def);
            Assert.IsNull(found);
            Assert.IsFalse(expect);

            found = PowerArgsRichCommandLineReader.FindCurrentTokenArgument(null, "-ActionInt", out expect, def);
            Assert.IsNull(found);
            Assert.IsTrue(expect);

            var action = new CommandLineAction((d) => { });
            action.Aliases.Add("TheAction");
            var actionArg = new CommandLineArgument(typeof(int), "ActionInt");
            action.Arguments.Add(actionArg);
            def.Actions.Add(action);

            found = PowerArgsRichCommandLineReader.FindCurrentTokenArgument(action, "-ActionInt", out expect, def);
            Assert.AreSame(actionArg, found);
            Assert.IsTrue(expect);
        }
    }
}
