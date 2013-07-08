using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class CaseSensitiveTests
    {
        
        public class ConflictingArgsParent
        {
            [ArgActionMethod]
            public void SomeAction(ConflictingArgsChild args) {}
        }

        [ArgEnforceCase]
        public class ConflictingArgsChild
        {
        }

        [ArgEnforceCase]
        public class ConflictingArgsParent2
        {
            [ArgActionMethod]
            public void SomeAction(ConflictingArgsChild2 args) { }
        }

        [ArgIgnoreCase]
        public class ConflictingArgsChild2
        {
        }


        [ArgIgnoreCase(false)]
        public class CaseSensitiveArgs
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            [ArgRequired]
            public string SomeOtherArg { get; set; }

            public SomeActionArgs TheActionArgs { get; set; }
            public static void TheAction(SomeActionArgs args) { }
        }

        [ArgEnforceCase]
        public class CaseSensitiveArgs2
        {
            public string SomeOtherArg { get; set; }
        }

        [ArgIgnoreCase(false)]
        [ArgEnforceCase]
        public class CaseSensitiveArgsInvalidDupeAttributes
        {
            public string SomeOtherArg { get; set; }
        }

        [ArgEnforceCase]  // Intentionally swapped from the order in the previous class
        [ArgIgnoreCase(false)]
        public class CaseSensitiveArgsInvalidDupeAttributesSwapped
        {
            public string SomeOtherArg { get; set; }
        }


        [ArgIgnoreCase(false)]
        public class CaseSensitiveArgsSC
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            [ArgRequired]
            public string SomeOtherArg { get; set; }

            public SomeActionArgs TheActionArgs { get; set; }
            public static void TheAction(SomeActionArgs args) { }
        }

        [ArgIgnoreCase(false)]
        public class SomeActionArgs
        {
            [ArgRequired]
            public int AnInteger { get; set; }
        }

        [TestMethod]
        public void TestConflictingCaseSensitivity()
        {
            Helpers.Run(() =>
            {
                Args.Parse<ConflictingArgsParent>();
            }
            , Helpers.ExpectedException<InvalidArgDefinitionException>());
        }

        [TestMethod]
        public void TestConflictingCaseSensitivity2()
        {
            Helpers.Run(() =>
            {
                Args.Parse<ConflictingArgsParent2>();
            }
            , Helpers.ExpectedException<InvalidArgDefinitionException>());
        }

        [TestMethod]
        public void TestCaseSensitivityPowerShellStyle()
        {
            Helpers.Run(() =>
            {
                var args = "TheAction -S SomeOtherArgValue -A 100".Split(' ');
                var parsed = Args.Parse<CaseSensitiveArgs>(args);

                Assert.AreEqual("SomeOtherArgValue", parsed.SomeOtherArg);
                Assert.AreEqual(100, parsed.TheActionArgs.AnInteger);
            });
        }

        [TestMethod]
        public void TestCaseSensitivityPowerShellStyleBadCasedShortcut()
        {
            Helpers.Run(() =>
            {
                var args = "TheAction -s SomeOtherArgValue -A 100".Split(' ');
                var parsed = Args.Parse<CaseSensitiveArgs>(args);

                Assert.AreEqual("SomeOtherArgValue", parsed.SomeOtherArg);
                Assert.AreEqual(100, parsed.TheActionArgs.AnInteger);
            }, Helpers.ExpectedArgException("required"));
        }

        [TestMethod]
        public void TestCaseSensitivityPowerShellStyleBadCasedAction()
        {
            Helpers.Run(() =>
            {
                var args = "Theaction -S SomeOtherArgValue -A 100".Split(' ');
                var parsed = Args.Parse<CaseSensitiveArgs>(args);

                Assert.AreEqual("SomeOtherArgValue", parsed.SomeOtherArg);
                Assert.AreEqual(100, parsed.TheActionArgs.AnInteger);
            }, Helpers.ExpectedException<UnknownActionArgException>("Unknown action: 'Theaction'"));
        }

        [TestMethod]
        public void TestCaseSensitivityPowerShellStyleBadCasedName()
        {
            Helpers.Run(() =>
            {
                var args = "TheAction -S SomeOtherArgValue -aninteger 100".Split(' ');
                var parsed = Args.Parse<CaseSensitiveArgs>(args);

                Assert.AreEqual("SomeOtherArgValue", parsed.SomeOtherArg);
                Assert.AreEqual(100, parsed.TheActionArgs.AnInteger);
            }, Helpers.ExpectedArgException("required"));
        }

        [TestMethod]
        public void TestCaseSensitivitySlashColonStyle()
        {
            Helpers.Run(()=>
            {
                var args = "TheAction /S:SomeOtherArgValue /A:100".Split(' ');

                var parsed = Args.Parse<CaseSensitiveArgsSC>(args);

                Assert.AreEqual("SomeOtherArgValue", parsed.SomeOtherArg);
                Assert.AreEqual(100, parsed.TheActionArgs.AnInteger);
            });
        }

        [TestMethod]
        public void TestCaseSensitivityIsCaseSensitiveAttribute()
        {
            try
            {
                var args = "/someOtherArg:SomeOtherArgValue".Split(' ');
                var parsed = Args.Parse<CaseSensitiveArgs2>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException expected) { }
        }

        [TestMethod]
        public void TestInvalidMultipleAttributes()
        {
            try
            {
                var args = "/SomeOtherArg:SomeOtherArgValue".Split(' ');
                var parsed = Args.Parse<CaseSensitiveArgsInvalidDupeAttributes>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException expected) 
            {
                Assert.IsTrue(expected.Message.ToLower().Contains("more than once"));
            }
        }
    }
}
