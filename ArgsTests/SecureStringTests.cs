using System;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class SecureStringTests
    {
        public class TestArgs
        {
            public SecureStringArgument Password { get; set; }
        }

        public class InvalidTestArgs
        {
            [ArgRequired]
            public SecureStringArgument Password { get; set; }
        }

        [TestMethod]
        public void TestSecureStringBasic()
        {
            TestConsoleProvider.SimulateConsoleInput("mysecretpassword");
            var parsed = Args.Parse<TestArgs>();
            Assert.AreEqual("mysecretpassword", parsed.Password.ConvertToNonsecureString());
        }

        [TestMethod]
        public void TestSecureStringNoValidatorsAllowed()
        {
            try
            {
                ConsoleProvider.Current = new StdConsoleProvider();
                var parsed = Args.Parse<InvalidTestArgs>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                // Expected
            }
        }

        [TestMethod]
        public void TestSecureStringNotPromptedUnlessNeeded()
        {
            ConsoleProvider.Current = new StdConsoleProvider();
            var parsed = Args.Parse<TestArgs>();

            var secureField = typeof(SecureStringArgument).GetField("secureString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(secureField);
            var secureFieldValue = secureField.GetValue(parsed.Password);
            Assert.IsNull(secureFieldValue);
        }

        [TestMethod]
        public void TestSecureStringNoCommandLine()
        {
            try
            {
                TestConsoleProvider.SimulateConsoleInput("mysecretpassword");
                var parsed = Args.Parse<TestArgs>("-p", "passwordFromCommandLine");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("cannot be specified on the command line"));
            }
        }

        [TestMethod]
        public void TestSecureStringIsReadOnly()
        {
            try
            {
                TestConsoleProvider.SimulateConsoleInput("mysecretpassword");
                var parsed = Args.Parse<TestArgs>();
                parsed.Password.SecureString.AppendChar('$');
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidOperationException ex)
            {
                // Expected
            }
        }
    } 
}
