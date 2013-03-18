using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Text.RegularExpressions;

namespace ArgsTests
{

    [TestClass]
    public class ShortcutTests
    {
        public class ShortcutArgs
        {
            public string SomeString { get; set; }
            public string OtherString { get; set; }
        }

        public class ShortcutArgsIgnoreLeadingDash
        {
            [ArgShortcut("-so")] // The leading dash here should be ignored
            public string SomeString { get; set; }
            public string OtherString { get; set; }
        }


        public class DuplicateShortcutArgs
        {
            public string SomeString { get; set; }
            public string SomeOtherString { get; set; }
        }

        public class DuplicateShortcutArgs2
        {
            [ArgShortcut("a")]
            public string SomeString { get; set; }
            [ArgShortcut("a")]
            public string OtherString { get; set; }
        }

        public class ArgShortcutAttributeArgs
        {
            public string SomeString { get; set; }
            [ArgShortcut("so")]
            public string SomeOtherString { get; set; }
        }

        public class ArgShortcutAttributeArgsNoShortcut
        {
            public string SomeString { get; set; }
            [ArgShortcut(null)]
            public string SomeOtherString { get; set; }
        }

        [TestMethod]
        public void TestBasicShortcuts()
        {
            var args = new string[] { "-s", "FirstString", "-o", "SecondString" };
            var parsed = Args.Parse<ShortcutArgs>(args);

            Assert.AreEqual("FirstString", parsed.SomeString);
            Assert.AreEqual("SecondString", parsed.OtherString);
        }

        [TestMethod]
        public void TestShortcutsWithAttributes()
        {
            var args = new string[] { "-s", "FirstString", "-so", "SecondString" };
            var parsed = Args.Parse<ArgShortcutAttributeArgs>(args);

            Assert.AreEqual("FirstString", parsed.SomeString);
            Assert.AreEqual("SecondString", parsed.SomeOtherString);
        }


        [TestMethod]
        public void TestNoShortcutsWithAttributes()
        {
            var args = new string[] { "-s", "FirstString", "-SomeOtherString", "SecondString" };
            var parsed = Args.Parse<ArgShortcutAttributeArgsNoShortcut>(args);

            Assert.AreEqual("FirstString", parsed.SomeString);
            Assert.AreEqual("SecondString", parsed.SomeOtherString);
        }


        [TestMethod]
        public void TestIgnoreLeadingDashes()
        {
            var args = new string[] { "-so", "FirstString", "-o", "SecondString" };
            var parsed = Args.Parse<ShortcutArgsIgnoreLeadingDash>(args);

            Assert.AreEqual("FirstString", parsed.SomeString);
            Assert.AreEqual("SecondString", parsed.OtherString);
        }


        [TestMethod]
        public void TestDuplicateArgs()
        {
            var args = new string[] { };
            var parsed = Args.Parse<DuplicateShortcutArgs>(args);
            Assert.AreEqual("s", ArgShortcut.GetShortcut(typeof(DuplicateShortcutArgs).GetProperty("SomeString")));
            Assert.AreEqual("so", ArgShortcut.GetShortcut(typeof(DuplicateShortcutArgs).GetProperty("SomeOtherString")));
        }

        [TestMethod]
        public void TestDuplicateArgsUsingShortcut()
        {
            try
            {
                var args = new string[] { };
                var parsed = Args.Parse<DuplicateShortcutArgs2>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("duplicate"));
            }
        }
    }
}
