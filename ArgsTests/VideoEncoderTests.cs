using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.IO;

namespace ArgsTests
{
    [TestClass]
    public class VideoEncoderTests
    {
        [ArgExample("superencoder encode fromFile toFile -encoder Wmv", "Encode the file at 'fromFile' to an AVI at 'toFile'")]
        public class VideoEncoderArgs
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            public EncodeArgs EncodeArgs { get; set; }
            public ClipArgs ClipArgs { get; set; }

            public bool WhatIf { get; set; }

            public static void Encode(EncodeArgs args)
            {
                args.Callback.Invoke();
            }

            public static void Clip(ClipArgs args) { args.Callback.Invoke(); }
        }

        public enum Encoder
        {
            Mpeg,
            Avi,
            Wmv
        }

        public class EncodeArgs
        {
            [ArgRequired]
            [ArgExistingFile]
            [ArgPosition(1)]
            public string Source { get; set; }

            [ArgPosition(2)]
            public string Output { get; set; }

            [DefaultValue(Encoder.Avi)]
            public Encoder Encoder { get; set; }

            [ArgIgnore]
            public Action Callback { get; set; }
        }

        public class ClipArgs : EncodeArgs
        {
            [ArgRange(0, 1000)]
            public double From { get; set; }
            [ArgRange(0, 1000)]
            public double To { get; set; }
        }

        [TestMethod]
        public void TestEncoderArgs()
        {
            var temp = Path.GetTempFileName();
            var outFileName = "outputFileName";
            var args = new string[] { "encode", temp, outFileName };

            var parsed = Args.ParseAction<VideoEncoderArgs>(args);

            Assert.IsNull(parsed.Args.ClipArgs);
            Assert.IsNotNull(parsed.Args.EncodeArgs);
            Assert.AreEqual(temp, parsed.Args.EncodeArgs.Source);
            Assert.AreEqual(outFileName, parsed.Args.EncodeArgs.Output);
            Assert.AreEqual(Encoder.Avi, parsed.Args.EncodeArgs.Encoder);

            bool called = false;
            parsed.Args.EncodeArgs.Callback = () =>
            {
                called = true;
            };

            Assert.IsFalse(called);
            parsed.Invoke();
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void TestUnknownAction()
        {
            var actionName = "wrongAction";
            var args = new string[] { actionName };
            try
            {
                var parsed = Args.ParseAction<VideoEncoderArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException e)
            {
                Assert.IsTrue(e.ToString().Contains(actionName), "Exception should mention the invalid action\n\n" + e.ToString());
            }
        }

        [TestMethod]
        public void TestActionArgInheritence()
        {
            var temp = Path.GetTempFileName();
            var outFileName = "outputFileName";
            double from = 10, to = 20;
            var args = new string[] { "clip", temp, outFileName, "-from", from + "", "-to", to + "" };

            var parsed = Args.ParseAction<VideoEncoderArgs>(args);

            Assert.IsNull(parsed.Args.EncodeArgs);
            Assert.IsNotNull(parsed.Args.ClipArgs);
            Assert.AreEqual(temp, parsed.Args.ClipArgs.Source);
            Assert.AreEqual(outFileName, parsed.Args.ClipArgs.Output);
            Assert.AreEqual(Encoder.Avi, parsed.Args.ClipArgs.Encoder);
            Assert.AreEqual(from, parsed.Args.ClipArgs.From);
            Assert.AreEqual(to, parsed.Args.ClipArgs.To);

            bool called = false;
            parsed.Args.ClipArgs.Callback = () =>
            {
                called = true;
            };

            Assert.IsFalse(called);
            parsed.Invoke();
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void TestVideoEncoderToolUsage()
        {
            var usage = ArgUsage.GetUsage<VideoEncoderArgs>(ArgStyle.PowerShell, "vidmaster");
            Console.WriteLine(usage);
        }
    }
}
