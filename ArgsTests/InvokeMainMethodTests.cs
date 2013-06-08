using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    public class MainArgs
    {
        [ArgIgnore]
        public bool MainWasCalled { get; private set; }

        [ArgRequired]
        public string StringVal { get; set; }

        public void Main()
        {
            Assert.AreEqual("hello", this.StringVal);
            MainWasCalled = true;
        }
    }

    public class MainArgsMissingMethod
    {
        [ArgRequired]
        public string StringVal { get; set; }
    }

    public class MainArgsStaticMethod
    {
        [ArgRequired]
        public string StringVal { get; set; }

        public static void Main()
        {
            Assert.Fail("This method is invalid and should never be called.");
        }
    }

    public class MainArgsMethodWithBadParams
    {
        [ArgRequired]
        public string StringVal { get; set; }

        public void Main(MainArgsMethodWithBadParams someParams)
        {
            Assert.Fail("This method is invalid and should never be called.");
        }
    }

    public class MainArgsMethodWithBadReturnType
    {
        [ArgRequired]
        public string StringVal { get; set; }

        public object Main()
        {
            Assert.Fail("This method is invalid and should never be called.");
            return null;
        }
    }

    [TestClass]
    public class InvokeMainMethodTests
    {
        [TestMethod]
        public void TestInvokeMainBasicStrong()
        {
            var result = Args.InvokeMain<MainArgs>("-s", "hello");
            Assert.IsTrue(result.Args.MainWasCalled);
            Assert.AreEqual("hello", result.Args.StringVal);
        }

        [TestMethod]
        public void TestInvokeMainBasicWeak()
        {
            var result = Args.InvokeMain(typeof(MainArgs), "-s", "hello");
            Assert.IsTrue(((MainArgs)result.Value).MainWasCalled);
            Assert.AreEqual("hello", ((MainArgs)result.Value).StringVal);
        }

        [TestMethod]
        public void TestInvokeMainNoMain()
        {
            try
            {
                var result = Args.InvokeMain<MainArgsMissingMethod>("-s", "hello");
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("no Main() method"));
            }
        }

        [TestMethod]
        public void TestInvokeMainStatic()
        {
            try
            {
                var result = Args.InvokeMain<MainArgsStaticMethod>("-s", "hello");
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("must not be static"));
            }
        }

        [TestMethod]
        public void TestInvokeMainBadParams()
        {
            try
            {
                var result = Args.InvokeMain<MainArgsMethodWithBadParams>("-s", "hello");
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("must not take any parameters"));
            }
        }

        [TestMethod]
        public void TestInvokeMainBadReturnType()
        {
            try
            {
                var result = Args.InvokeMain<MainArgsMethodWithBadReturnType>("-s", "hello");
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("must return void"));
            }
        }
    }
}
