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

        public class SomeActionArgs
        {
            [ArgRequired]
            public int AnInteger { get; set; }
        }

        private static ArgOptions CaseSensitiveOptions
        {
            get
            {
                return new ArgOptions()
                { 
                    IgnoreCaseForPropertyNames = false,
                    Style = ArgStyle.PowerShell
                };
            }
        }


        [TestMethod]
        public void TestCaseSensitivityPowerShellStyle()
        {
            Helpers.Run(() =>
            {
                var args = "TheAction -S SomeOtherArgValue -A 100".Split(' ');
                var parsed = Args.Parse<CaseSensitiveArgs>(CaseSensitiveOptions, args);

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
                var parsed = Args.Parse<CaseSensitiveArgs>(CaseSensitiveOptions, args);

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
                var parsed = Args.Parse<CaseSensitiveArgs>(CaseSensitiveOptions, args);

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
                var parsed = Args.Parse<CaseSensitiveArgs>(CaseSensitiveOptions, args);

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

                var options = CaseSensitiveOptions;
                options.Style = ArgStyle.SlashColon;
                var parsed = Args.Parse<CaseSensitiveArgs>(options, args);

                Assert.AreEqual("SomeOtherArgValue", parsed.SomeOtherArg);
                Assert.AreEqual(100, parsed.TheActionArgs.AnInteger);
            });
        }
    }
}
