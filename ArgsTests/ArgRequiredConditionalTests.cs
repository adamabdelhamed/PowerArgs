using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests
{
    [TestClass]
    public class ArgRequiredConditionalTests
    {
        public class SimpleUnlessArgs
        {
            [ArgRequired(IfNot = "SecondArgument"), ArgCantBeCombinedWith("SecondArgument")]
            public string FirstArgument { get; set; }

            [ArgShortcut("-s")]
            public string SecondArgument { get; set; }
        }

        public class SimpleUnlessArgsWithTypo
        {
            [ArgRequired(IfNot = "SecondArgumentABC")]
            public string FirstArgument { get; set; }

            [ArgShortcut("-s")]
            public string SecondArgument { get; set; }
        }

        public class SimpleUnlessArgsWithLogicError
        {
            [ArgRequired(IfNot = "SecondArgument || SecondArgument")]
            public string FirstArgument { get; set; }

            [ArgShortcut("-s")]
            public string SecondArgument { get; set; }
        }

        public class UnexclusiveUnlessArgs
        {
            [ArgRequired(IfNot = "SecondArgument")]
            public string FirstArgument { get; set; }

            [ArgShortcut("-s")]
            public string SecondArgument { get; set; }
        }

        public class OneOfThreeUnlessArgs
        {
            [ArgRequired(IfNot = "SecondArgument | ThirdArgument"), ArgCantBeCombinedWith("SecondArgument | ThirdArgument")]
            public string FirstArgument { get; set; }

            [ArgShortcut("-s")]
            public string SecondArgument { get; set; }
                
            [ArgShortcut("-t")]
            public string ThirdArgument { get; set; }
        }

        public class ComplexUnlessArgs
        {
            [ArgRequired(  IfNot = "LocalFile & LocalFileUserName & LocalFilePassword")]
            [ArgCantBeCombinedWith("LocalFile & LocalFileUserName & LocalFilePassword")]
            public string Uri { get; set; }

            public string LocalFile { get; set; }
            public string LocalFileUserName { get; set; }
            public string LocalFilePassword { get; set; }
        }

        public class DependentRequiredIfArgs
        {
            public string LogFileOutputPath { get; set; }

            [ArgRequired(If = "LogFileOutputPath")]
            public long LogFileOutputMaxSize { get; set; }
        }

        public class DependentRequiredIfArgsWithNot
        {
            public bool FastTrack { get; set; }

            [ArgRequired(If = "!FastTrack")]
            public string SlowInput { get; set; }
        }
        
        [TestMethod]
        public void TestSimpleUnlessArgsWithTypo()
        {
            SimpleUnlessArgsWithTypo parsed;

            try
            {
                parsed = Args.Parse<SimpleUnlessArgsWithTypo>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("not a valid argument alias"));
                Assert.IsTrue(ex.Message.Contains("SecondArgumentABC"));
            }
        }

        [TestMethod]
        public void TestSimpleUnlessArgsWithLogicError()
        {
            SimpleUnlessArgsWithLogicError parsed;

            try
            {
                parsed = Args.Parse<SimpleUnlessArgsWithLogicError>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("||"));
            }
        }

        [TestMethod]
        public void TestDependentRequiredIfWithNots()
        {
            DependentRequiredIfArgsWithNot parsed;

            parsed = Args.Parse<DependentRequiredIfArgsWithNot>("-f");
            Assert.IsTrue(parsed.FastTrack);
            Assert.IsNull(parsed.SlowInput);

            parsed = Args.Parse<DependentRequiredIfArgsWithNot>("-s", "SomeInput");
            Assert.IsFalse(parsed.FastTrack);
            Assert.AreEqual("SomeInput",parsed.SlowInput);

            try
            {
                parsed = Args.Parse<DependentRequiredIfArgsWithNot>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (MissingArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("required"));
            }
        }

        [TestMethod]
        public void TestDependentRequiredIf()
        {
            DependentRequiredIfArgs parsed;

            parsed = Args.Parse<DependentRequiredIfArgs>();
            Assert.IsNull(parsed.LogFileOutputPath);
            Assert.AreEqual(0,parsed.LogFileOutputMaxSize);

            parsed = Args.Parse<DependentRequiredIfArgs>("-LogFileOutputPath", "SomePath", "-LogFileOutputMaxSize", "1024");
            Assert.AreEqual("SomePath", parsed.LogFileOutputPath);
            Assert.AreEqual(1024, parsed.LogFileOutputMaxSize);

            try
            {
                parsed = Args.Parse<DependentRequiredIfArgs>("-LogFileOutputPath", "SomePath");
                Assert.Fail("An exception should have been thrown");
            }
            catch(MissingArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("required"));
            }
        }

        [TestMethod]
        public void TestArgRequiredUnlessBasic()
        {
            SimpleUnlessArgs parsed;

            parsed = Args.Parse<SimpleUnlessArgs>("-f", "Adam");
            Assert.IsNotNull(parsed.FirstArgument);
            Assert.IsNull(parsed.SecondArgument);

            parsed = Args.Parse<SimpleUnlessArgs>("-s", "Adam");
            Assert.IsNull(parsed.FirstArgument);
            Assert.IsNotNull(parsed.SecondArgument);

            try
            {
                parsed = Args.Parse<SimpleUnlessArgs>("-f", "Adam", "-s", "Joe");
                Assert.Fail("An exception should have been thrown");
            }
            catch (UnexpectedArgException ex)
            {

            }

            try
            {
                parsed = Args.Parse<SimpleUnlessArgs>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (MissingArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("required"));
            }
        }

        [TestMethod]
        public void TestArgRequiredUnlessOneOfThree()
        {
            OneOfThreeUnlessArgs parsed;

            parsed = Args.Parse<OneOfThreeUnlessArgs>("-f", "Adam");
            Assert.IsNotNull(parsed.FirstArgument);
            Assert.IsNull(parsed.SecondArgument);
            Assert.IsNull(parsed.ThirdArgument);

            parsed = Args.Parse<OneOfThreeUnlessArgs>("-s", "Adam");
            Assert.IsNull(parsed.FirstArgument);
            Assert.IsNotNull(parsed.SecondArgument);
            Assert.IsNull(parsed.ThirdArgument);

            parsed = Args.Parse<OneOfThreeUnlessArgs>("-t", "Adam");
            Assert.IsNull(parsed.FirstArgument);
            Assert.IsNull(parsed.SecondArgument);
            Assert.IsNotNull(parsed.ThirdArgument);

            try
            {
                parsed = Args.Parse<OneOfThreeUnlessArgs>("-f", "Adam", "-t", "Frank");
                Assert.Fail("An exception should have been thrown");
            }
            catch(UnexpectedArgException ex)
            {

            }

            try
            {
                parsed = Args.Parse<OneOfThreeUnlessArgs>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (MissingArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("required"));
            }
        }

        [TestMethod]
        public void TestComplexRequiredUnless()
        {
            ComplexUnlessArgs parsed;

            parsed = Args.Parse<ComplexUnlessArgs>("-LocalFile", "SomeFilePath", "-LocalFileUserName", "Adam", "-LocalFilePassword","SomePassword");
            Assert.IsNull(parsed.Uri);

            Assert.AreEqual("SomeFilePath", parsed.LocalFile);
            Assert.AreEqual("Adam", parsed.LocalFileUserName);
            Assert.AreEqual("SomePassword", parsed.LocalFilePassword);

            try
            {
                parsed = Args.Parse<ComplexUnlessArgs>("-LocalFile", "SomeFilePath");
                Assert.Fail("An exception should have been thrown");
            }
            catch(Exception ex)
            {

            }
        }
    }     
}
