using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests
{
    [TestClass]
    public class ArgRequiredUnlessTests
    {
        public class SimpleUnlessArgs
        {
            [ArgRequired(Unless = "SecondArgument")]
            public string FirstArgument { get; set; }

            [ArgShortcut("-s")]
            public string SecondArgument { get; set; }
        }

        public class UnexclusiveUnlessArgs
        {
            [ArgRequired(Unless = "SecondArgument",UnlessIsExclusive=false)]
            public string FirstArgument { get; set; }

            [ArgShortcut("-s")]
            public string SecondArgument { get; set; }
        }

        public class OneOfThreeUnlessArgs
        {
            [ArgRequired(Unless = "SecondArgument|ThirdArgument")]
            public string FirstArgument { get; set; }

            [ArgShortcut("-s")]
            public string SecondArgument { get; set; }
                
            [ArgShortcut("-t")]
            public string ThirdArgument { get; set; }
        }

        public class ComplexUnlessArgs
        {
            [ArgRequired(Unless = "LocalFile&LocalFileUserName&LocalFilePassword", UnlessDescription="local file info is supplied", UnlessIsExclusive = false)]
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
