using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

using System.Linq;


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
            destination.Metadata.Add(new ArgRequired());
            destination.Description = "The place to go to";

            action.Arguments.Add(destination);

            Args.InvokeAction(definition, "go", "-destination", "Hawaii");
            Assert.IsTrue(invoked);

            var usage = ArgUsage.GetUsage(definition, "test");
            Assert.IsTrue(usage.Contains("A simple action"));
            Assert.IsTrue(usage.Contains("The place to go to"));
        }

        [TestMethod]
        public void TestConflictingIsRequiredOverride()
        {
            TestConflictingOverride((argument) =>
            {
                argument.IsRequired = false;
                argument.Metadata.Add(new ArgRequired());
            }, "IsRequired");
        }

        [TestMethod]
        public void ValidateNoDanglingAttributes()
        {
            // This list represents attributes that have been cleared to ship without deriving
            // from ArgMetadata.  This test is here to make sure that, by default, the attributes
            // in this project behave like most of the others.  That is, most attributes should
            // derive from ArgMetadata.
            var whitelist = new Type[]
            {
                typeof(ArgReviverAttribute),
                typeof(DynamicExpressionProviderAttribute),
                typeof(ArgActions),
            };

            var iArgMetadataSubInterfaces = typeof(Args).Assembly.GetTypes().Where(t =>
                t.IsInterface &&
                t.GetInterfaces().Contains(typeof(IArgMetadata))).ToList();

            // In general, attributes in this project should derive from ArgMetadata
            var danglingAttrs = typeof(Args).Assembly.GetTypes().Where(t => 
                t.IsSubclassOf(typeof(Attribute))   == true &&
                t.GetInterfaces().Contains(typeof(IArgMetadata)) == false && 
                whitelist.Contains(t) == false
            ).ToList();

            // In general, attibutes should use more specific IArgMetadata interfaces
            var danglingAttrs2 = typeof(Args).Assembly.GetTypes().Where(t =>
                t.GetInterfaces().Contains(typeof(Attribute)) == true &&
                (from i in t.GetInterfaces() where iArgMetadataSubInterfaces.Contains(i) select i).Count() == 0 &&
                whitelist.Contains(t) == false
            ).ToList();


            Assert.AreEqual(0, danglingAttrs.Count);
            Assert.AreEqual(0, danglingAttrs2.Count);
        }

        [TestMethod]
        public void TestConflictingAliasDefinitions()
        {
            CommandLineArgumentsDefinition definition = new CommandLineArgumentsDefinition();
            var argument = new CommandLineArgument(typeof(int), "somenumber");
            argument.Metadata.Add(new ArgShortcut("some"));

            try
            {
                argument.Aliases.Add("some");
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.AreEqual(ex.Message, "The alias 'some' has already been added");
            }
        }

        [TestMethod]
        public void TestConflictingDescriptionOverride()
        {
            TestConflictingOverride((argument) =>
            {
                argument.Description = "Foo";
                argument.Metadata.Add(new ArgDescription("Bar"));
            }, "Description");
        }

        [TestMethod]
        public void TestConflictingPositionOverride()
        {
            TestConflictingOverride((argument) =>
            {
                argument.Position = 1;
                argument.Metadata.Add(new ArgPosition(0));
            }, "Position");
        }

        [TestMethod]
        public void TestConflictingDefaultValueOverride()
        {
            TestConflictingOverride((argument) =>
            {
                argument.DefaultValue = 0;
                argument.Metadata.Add(new DefaultValueAttribute(1));
            }, "DefaultValue");
        }

        [TestMethod]
        public void TestConflictingIgnoreCaseOverride()
        {
            TestConflictingOverride((argument) =>
            {
                argument.IgnoreCase = true;
                argument.Metadata.Add(new ArgIgnoreCase(false));
            }, "IgnoreCase");
        }

        public void TestConflictingOverride(Action<CommandLineArgument> variation, string errorMessageExpectedContents)
        {
            CommandLineArgumentsDefinition definition = new CommandLineArgumentsDefinition();
            var argument = new CommandLineArgument(typeof(int), "somenumber");
            definition.Arguments.Add(argument);
            variation(argument);
            try
            {
                Args.Parse(definition, "-somenumber", "100");
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.Contains(errorMessageExpectedContents));
            }
        }
    }
}
