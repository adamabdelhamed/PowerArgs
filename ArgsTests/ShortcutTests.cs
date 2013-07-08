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
        public enum ForecastType
        {
            Today,
            [ArgShortcut("2Day")]
            TwoDay,
            [ArgShortcut("5Day")]
            FiveDay,
            Week,
        }

        public enum EnumWithDupeShortcuts
        {
            Today,
            [ArgShortcut("ABC")]
            TwoDay,
            [ArgShortcut("ABC")]
            FiveDay,
            Week,
        }

        public class EnumShortcutArgs
        {
            public ForecastType Type { get; set; }
        }

        public class ShortcutOnlyArgs
        {
            [ArgShortcut(ArgShortcutPolicy.ShortcutsOnly),ArgShortcut("-f")]
            public string Foo { get; set; }
        }

        public class EnumWithDupeShortcutsArgs
        {
            public EnumWithDupeShortcuts Bad { get; set; }
        }

        public class ShortcutArgs
        {
            public string SomeString { get; set; }
            public string OtherString { get; set; }
        }

        public class MultipleShortcutArgs
        {
            [ArgShortcut("-h")]
            [ArgShortcut("-?")]
            [ArgShortcut("-??")]
            [ArgShortcut("--?")]
            [ArgShortcut("--get-help")]
            public bool Help { get; set; }
        }

        public class MultipleShortcutDuplicateArgs
        {
            [ArgShortcut("-h")]
            [ArgShortcut("-h")]
            public bool Help { get; set; }
        }


        public class ShortcutArgsIgnoreLeadingDash
        {
            [ArgShortcut("-so")] // The leading dash should be ignored
            public string SomeString { get; set; }
            [ArgShortcut("/o")] // The leading slash should be ignored
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

        public class DuplicateShortcutArgs3
        {
            [ArgShortcut("a")]
            public string SomeString { get; set; }
            [ArgShortcut("A")]
            public string OtherString { get; set; }
        }

        [ArgIgnoreCase(false)]
        public class DuplicateShortcutArgsCaseSensitive
        {
            [ArgShortcut("a")]
            public string AdditionalInfo { get; set; }
            [ArgShortcut("A")]
            public string Address { get; set; }
        }

        public class ShortcutArgsConflictingArguments
        {
            [ArgShortcut(ArgShortcutPolicy.NoShortcut), ArgShortcut("so")]
            public string SomeString { get; set; }
            public string OtherString { get; set; }
        }


        public class DuplicateShortcutEdgeCaseArgs
        {
            // This is the case where several properties have very similar names

            [ArgShortcut("ab")]
            public string Abcdefg0 { get; set; }
            public string Abcdefg1 { get; set; }
            public string Abcdefg2 { get; set; }
            public string Abcdefg3 { get; set; }
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

        public class ArgShortcutAttributeArgsNoShortcut2
        {
            public string SomeString { get; set; }
            [ArgShortcut(ArgShortcutPolicy.NoShortcut)]
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
            Assert.AreEqual("S", (typeof(DuplicateShortcutArgs).GetShortcut("SomeString")));
            Assert.AreEqual("So", (typeof(DuplicateShortcutArgs).GetShortcut("SomeOtherString")));
        }

        [TestMethod]
        public void TestDuplicateArgsEdge()
        {
            var args = new string[] { };
            var parsed = Args.Parse<DuplicateShortcutEdgeCaseArgs>(args);
            Assert.AreEqual("ab", (typeof(DuplicateShortcutEdgeCaseArgs).GetShortcut("Abcdefg0")));
            Assert.AreEqual("A", (typeof(DuplicateShortcutEdgeCaseArgs).GetShortcut("Abcdefg1")));
            Assert.AreEqual("Abc", (typeof(DuplicateShortcutEdgeCaseArgs).GetShortcut("Abcdefg2")));
            Assert.AreEqual("Abcd", (typeof(DuplicateShortcutEdgeCaseArgs).GetShortcut("Abcdefg3")));
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

        [TestMethod]
        public void TestDuplicateArgsUsingShortcutDifferentCases()
        {
            try
            {
                var args = new string[] { };
                var parsed = Args.Parse<DuplicateShortcutArgs3>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("duplicate"));
            }
        }

        [TestMethod]
        public void TestDuplicateArgsUsingShortcutDifferentCasesAllowed()
        {

            var args = new string[] { "-a", "Additional Info Value", "-A", "Address Value" };
            var parsed = Args.Parse<DuplicateShortcutArgsCaseSensitive>(args);

            Assert.AreEqual("Additional Info Value", parsed.AdditionalInfo);
            Assert.AreEqual("Address Value", parsed.Address);
        }

        [TestMethod]
        public void TestShortcutConflictingSignals()
        {
            try
            {
                var args = new string[] { "-so", "asdasd" };
                var parsed = Args.Parse<ShortcutArgsConflictingArguments>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException)
            {
 
            }
        }

        [TestMethod]
        public void TestMultipleShortcuts()
        {
            var parsed = Args.Parse<MultipleShortcutArgs>(new string[0]);
            Assert.AreEqual(false, parsed.Help);

            parsed = Args.Parse<MultipleShortcutArgs>("-h");
            Assert.AreEqual(true, parsed.Help);

            parsed = Args.Parse<MultipleShortcutArgs>("-?");
            Assert.AreEqual(true, parsed.Help);

            parsed = Args.Parse<MultipleShortcutArgs>("-??");
            Assert.AreEqual(true, parsed.Help);

            parsed = Args.Parse<MultipleShortcutArgs>("--?");
            Assert.AreEqual(true, parsed.Help);

            parsed = Args.Parse<MultipleShortcutArgs>("--get-help");
            Assert.AreEqual(true, parsed.Help);


            Assert.AreEqual(5, typeof(MultipleShortcutArgs).GetShortcuts("Help").Count);
        }

        [TestMethod]
        public void TestMultipleShortcutsWithDuplicates()
        {
            try
            {
                var parsed = Args.Parse<MultipleShortcutDuplicateArgs>(new string[0]);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("duplicate"));
            }
        }

        [TestMethod]
        public void TestEnumShortcuts()
        {
            var parsed = Args.Parse<EnumShortcutArgs>("-t", "2day");
            Assert.AreEqual(ForecastType.TwoDay, parsed.Type);
        }

        [TestMethod]
        public void TestEnumWithDupeShortcuts()
        {
            try
            {
                var parsed = Args.Parse<EnumWithDupeShortcutsArgs>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("duplicate"));
                Assert.IsTrue(ex.Message.Contains(typeof(EnumWithDupeShortcuts).Name));
            }
        }

        [TestMethod]
        public void TestShortcutsOnly()
        {
            try
            {
                Args.Parse<ShortcutOnlyArgs>("-Foo", "Value");
                Assert.Fail("An exception should have been thrown");
            }
            catch (UnexpectedArgException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Foo"));
            }

            var parsed = Args.Parse<ShortcutOnlyArgs>("-f", "Value");
            Assert.AreEqual("Value", parsed.Foo);
        }
    }
}
