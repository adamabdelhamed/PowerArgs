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
    [TestCategory(Categories.Core)]
    public class VideoEncoderTests
    {
        [ArgExample("superencoder encode fromFile toFile -encoder Wmv", "Encode the file at 'fromFile' to an AVI at 'toFile'")]
        public class VideoEncoderArgs
        {
            [ArgRequired]
            [ArgPosition(0)]
            [ArgDescription("Either encode or clip")]
            public string Action { get; set; }

            [ArgDescription("Encode a new video file")]
            public EncodeArgs EncodeArgs { get; set; }
            [ArgDescription("Save a portion of a video to a new video file")]
            public ClipArgs ClipArgs { get; set; }

            [ArgDescription("Simulate the encoding operation")]
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
            [ArgDescription("The source video file")]
            public string Source { get; set; }

            [ArgPosition(2)]
            [ArgDescription("Output file.  If not specfied, defaults to current directory")]
            public string Output { get; set; }

            [DefaultValue(Encoder.Avi)]
            [ArgDescription("The type of encoder to use")]
            public Encoder Encoder { get; set; }

            [ArgIgnore]
            public Action Callback { get; set; }
        }

        public class ClipArgs : EncodeArgs
        {
            [ArgRange(0, double.MaxValue)]
            [ArgDescription("The starting point of the video, in seconds")]
            public double From { get; set; }
            
            [ArgRange(0, double.MaxValue)]
            [ArgDescription("The ending point of the video, in seconds")]
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
    }
}

