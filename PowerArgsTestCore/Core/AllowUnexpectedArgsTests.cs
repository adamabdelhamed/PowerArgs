using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class AllowUnexpectedArgsTests
    {
        [AllowUnexpectedArgs]
        public class SomeArgsWithAllowAttribute
        {
            public string AnArg { get; set; }    
        }

        public class SomeArgsWithNoAllowAttribute
        {
            public string AnArg { get; set; }
        }

        [TestMethod]
        public void AllowUnexpectedArgsBasic()
        {
            var parsed = Args.Parse<SomeArgsWithAllowAttribute>("-dynamicArg", "dynamicValue");
            Assert.IsNull(parsed.AnArg);
            var ambientDefinition = Args.GetAmbientDefinition();
            Assert.AreEqual(1, ambientDefinition.UnexpectedExplicitArguments.Count);
            Assert.AreEqual(ambientDefinition.UnexpectedExplicitArguments["dynamicArg"], "dynamicValue");
        }

        [TestMethod]
        public void AllowUnexpectedArgsNotAllowed()
        {
            try
            {
                var parsed = Args.Parse<SomeArgsWithNoAllowAttribute>("-dynamicArg", "dynamicValue");
                Assert.Fail("An exception should have been thrownB");
            }
            catch (UnexpectedArgException)
            {

            }
        }
    }
}
