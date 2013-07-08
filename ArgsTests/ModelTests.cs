using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
namespace ArgsTests
{
    [TestClass]
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
            argument.Validators.Add(new ArgRequired());

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

            var usage = ArgUsage.GetUsage(definition, "test");

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
            destination.Validators.Add(new ArgRequired());
            destination.Description = "The place to go to";

            action.Arguments.Add(destination);

            Args.InvokeAction(definition, "go", "-destination", "Hawaii");
            Assert.IsTrue(invoked);

            var usage = ArgUsage.GetUsage(definition, "test");
            Assert.IsTrue(usage.Contains("A simple action"));
            Assert.IsTrue(usage.Contains("The place to go to"));
        }
    }
}
