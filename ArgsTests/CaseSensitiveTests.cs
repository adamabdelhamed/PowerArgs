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

        [ArgStyle(ArgStyle.SlashColon)]
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
            }, Helpers.ExpectedArgException("unknown action"));
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
    }
}
