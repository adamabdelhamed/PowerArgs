using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class EdgeCases
    {
        public enum EdgeEnum
        {
            Foo,
            Bar
        }

        public class BadPositionalArgs
        {
            [ArgPosition(-1)]
            public string Foo { get; set; }
        }

        public class StrangeShortcuts
        {
            [ArgShortcut("Foo")]
            public string Bar { get; set; }

            public string Foo { get; set; }
        }

        public class ConflictingShortcutPolicyArgsNoShortcutWithShortcut
        {
            [ArgShortcut(ArgShortcutPolicy.NoShortcut),ArgShortcut("-f")]
            public string Foo { get; set; }
        }

        public class ConflictingShortcutPolicyArgsNoShortcutShortcustOnly
        {
            [ArgShortcut(ArgShortcutPolicy.NoShortcut), ArgShortcut(ArgShortcutPolicy.ShortcutsOnly)]
            public string Foo { get; set; }
        }

        public class ConflictingShortcutPolicyArgsShortcutsOnlyNoShortcuts
        {
            [ArgShortcut(ArgShortcutPolicy.ShortcutsOnly)]
            public string Foo { get; set; }
        }

        public class StrangeShortcuts2
        {
            [ArgShortcut("Fo")]
            [ArgShortcut("F")]
            public string Bar { get; set; }

            public string Foo { get; set; }
        }

        public class CustomType
        {
            public int IntVal { get; set; }
        }

        public class NoReviverArgs
        {
            public CustomType Arg { get; set; }
        }

        public class BasicArgs
        {
            public string String { get; set; }
            public int Int { get; set; }
            public double Double { get; set; }
            public bool Bool { get; set; }

            public Guid Guid { get; set; }
            public DateTime Time { get; set; }
            public long Long { get; set; }
            [ArgShortcut("by")]
            public byte Byte { get; set; }

            public Uri Uri { get; set; }

            public string[] ArrayOfStrings { get; set; }
            [ArgShortcut("li")]
            public List<int> ListOfInts { get; set; }

            [ArgIgnore]
            public object SomeObjectToIgnore { get; set; }

            public EdgeEnum EdgeEnum { get; set; }
        }

        public class BasicArgsSC
        {
            public string String { get; set; }
            public int Int { get; set; }
            public double Double { get; set; }
            public bool Bool { get; set; }

            public Guid Guid { get; set; }
            public DateTime Time { get; set; }
            public long Long { get; set; }
            [ArgShortcut("by")]
            public byte Byte { get; set; }

            [ArgIgnore]
            public object SomeObjectToIgnore { get; set; }
        }

        [TestMethod]
        public void TestNegativePosition()
        {
            try
            {
                Args.Parse<BadPositionalArgs>("-f", "blah");
                Assert.Fail("An exception should have been thrown.");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.Contains(">= 0"));
            }
        }

        [TestMethod]
        public void TestShortcutGenerationEdgeCase()
        {
            try
            {
                var parsed = Args.Parse<StrangeShortcuts>("-foo", "value");
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("duplicate"));
            }
        }

        [TestMethod]
        public void TestShortcutGenerationEdgeCase2()
        {
            var def = new CommandLineArgumentsDefinition(typeof(StrangeShortcuts2));

            Assert.AreEqual(1, def.Arguments[1].Aliases.Count);
            Assert.AreEqual("Foo", def.Arguments[1].DefaultAlias);
        }

        [TestMethod]
        public void TestNoReviver()
        {
            try
            {
                Args.Parse<NoReviverArgs>(new string[0]);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.Contains(typeof(CustomType).Name));
            }
        }

        [TestMethod]
        public void TestBadInt()
        {
            TestBadValues("-i", 1.1 + "");
            TestBadValues("-i", "abc");
        }

        [TestMethod]
        public void TestBadByte()
        {
            TestBadValues("-by", 1.1 + "");
            TestBadValues("-by", "-1");
            TestBadValues("-by", "256");
            TestBadValues("-by", "abc");
        }

        [TestMethod]
        public void TestBadDouble()
        {
            TestBadValues("-d", "abc");
        }

        [TestMethod]
        public void TestBadUri()
        {
            TestBadValues("-u", "http//www.bing.com"); // Missing a colon after http
        }

        [TestMethod]
        public void TestBadLong()
        {
            TestBadValues("-l", 1.1 + "");
            TestBadValues("-l", "abc");
        }

        [TestMethod]
        public void TestBadGuid()
        {
            TestBadValues("-g", "sdfdsfsdf");
        }

        [TestMethod]
        public void TestBadDateTime()
        {
            TestBadValues("-t", "sdfdsfsdf");
        }


        public void TestBadValues(string shortcut, string variation)
        {
            var args = new string[] 
            { 
                "-s", "stringValue", 
                "-i", "34", 
                "-d", "33.33", 
                "-b", 
                "-by", "255", 
                "-g", Guid.NewGuid().ToString(), 
                "-t", DateTime.Today.ToString(), 
                "-l", long.MaxValue + "",
                "-u", "http://www.bing.com"
            };

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == shortcut)
                {
                    args[i + 1] = variation;
                    break;
                }
            }

            try
            {
                BasicArgs parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException ex)
            {
                Assert.IsNotNull(ex.InnerException, "Missing inner exception from revier");
                Assert.IsInstanceOfType(ex.InnerException, typeof(FormatException));

                Assert.IsTrue(ex.Message.Contains(variation));
            }
        }

        [TestMethod]
        public void TestBadArgFormatsPowerShellStyle()
        {
            try
            {
                var args = new string[] { "-" };
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("missing"));
            }
        }

        [TestMethod]
        public void TestBadArgFormatsSlashcolonStyle()
        {
            try
            {
                var args = new string[] { "/" };
                var parsed = Args.Parse<BasicArgsSC>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("missing"));
            }
        }

        [TestMethod]
        public void TestBadListInput()
        {
            Helpers.Run(() =>
            {
                var args = "-li ,".Split(' ');
                var parsed = Args.Parse<BasicArgs>(args);
            }, Helpers.ExpectedArgException());
        }

        [TestMethod]
        public void TestEmptyArrayInput()
        {
            Helpers.Run(() =>
            {
                var args = "-a ".Split(' ');
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.IsNotNull(parsed.ArrayOfStrings);
                Assert.AreEqual(0, parsed.ArrayOfStrings.Length);
            });
        }

        [TestMethod]
        public void TestStrangeArrayInput()
        {
            Helpers.Run(() =>
            {
                var args = "-a ,".Split(' ');
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.IsNotNull(parsed.ArrayOfStrings);
                Assert.AreEqual(2, parsed.ArrayOfStrings.Length);
                Assert.AreEqual("", parsed.ArrayOfStrings[0]);
                Assert.AreEqual("", parsed.ArrayOfStrings[1]);
            });
        }

        [TestMethod]
        public void TestEmptyEnum()
        {
            Helpers.Run(() =>
            {
                var parsed = Args.Parse<BasicArgs>("-edgeenum");
            }, Helpers.ExpectedArgException(expectedText: "<empty> is not a valid value for type EdgeEnum, options are Foo, Bar"));
        }

        [TestMethod]
        public void TestConflictingShortcutPolicies()
        {
            try
            {
                Args.Parse<ConflictingShortcutPolicyArgsNoShortcutWithShortcut>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex) { }

            try
            {
                Args.Parse<ConflictingShortcutPolicyArgsNoShortcutShortcustOnly>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex) { }

            try
            {
                Args.Parse<ConflictingShortcutPolicyArgsShortcutsOnlyNoShortcuts>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex) { }
        }
    }
}
