using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [ArgExample("superencoder encode fromFile toFile -encoder Wmv", "Encode the file at 'fromFile' to an AVI at 'toFile'")]
    public class VideoEncoderArgs
    {
        // To use the action framework, the action arg must be called "Action"
        // and must be a required parameter at position 0.
        //
        // If you're using the Action Framework then don't use Args.Parse<VideoEncoderArgs>(args).
        // Instead use Args.InvokeAction<VideoEncoderArgs>(args).  That will not only parse the
        // arguments, but it will also map the user's specified action to an action property,
        // populate that property, and finally invoke the action method.
        // There is an example of this below.


        [ArgRequired]
        [ArgPosition(0)]
        [ArgDescription("Either encode or clip")]
        public string Action { get; set; }

        // See the two properties below.  They are action properties.  If 
        // your class has the "Action" property configured correctly then all
        // remaining properties that end with "Args" will be considered actions
        // that the user can enter as their first command line value.
        // 
        // In this case the end user could enter "superencoder encode" or
        // "superencode clip".  Based on the action parameter the rest of the
        // arguments will be used to populate the matching action property.

        [ArgDescription("Encode a new video file")]
        public EncodeArgs EncodeArgs { get; set; }

        [ArgDescription("Save a portion of a video to a new video file")]
        public ClipArgs ClipArgs { get; set; }

        public static void Encode(EncodeArgs args)
        {
            // TODO - Your action code
        }

        public static void Clip(ClipArgs args)
        {
            // TODO - Your action code
        }
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


    [TestClass]
    public class ReadMeTests
    {
        [TestMethod]
        public void TestUsageAsActionFx()
        {
            var usage = ArgUsage.GetUsage<VideoEncoderArgs>("test");
            Assert.IsTrue(usage.Contains("Actions:"));

        }
    }
}
