using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
using System.Linq;

namespace ArgsTests
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling, ExeName="UnitTests")]
    public class SimpleArgs
    {
        [ArgRequired]
        public string StringThatsRequired { get; set; }
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling, ExeName = "UnitTests")]
    public class SourceControlArgs
    {
        [ArgActionMethod]
        public void Push(PushPullArgs args)
        {
            AmbientArgsAndExceptionHandlingTests.Parsed.Add(args);
        }

        [ArgActionMethod]
        public void Pull(PushPullArgs args)
        {
            AmbientArgsAndExceptionHandlingTests.Parsed.Add(args);
        }
    }

    public class PushPullArgs
    {
        [ArgPosition(1)]
        [ArgRequired]
        public string Remote { get; set; }

        [ArgPosition(2)]
        public string Branch { get; set; }
    }

    [TestClass]
    public class AmbientArgsAndExceptionHandlingTests
    {
        public static List<PushPullArgs> Parsed = new List<PushPullArgs>();

        [TestMethod]
        public void TestExceptionBehaviorBasicErrorPath()
        {
            var ret = Args.Parse<SimpleArgs>();
            Assert.IsNull(ret);
            Assert.IsNull(Args.GetAmbientArgs<SimpleArgs>());
        }

        [TestMethod]
        public void TestExceptionBehaviorBasicHappyPath()
        {
            var ret = Args.Parse<SimpleArgs>("-s", "value");
            Assert.AreEqual("value", ret.StringThatsRequired);
            Assert.AreEqual("value", Args.GetAmbientArgs<SimpleArgs>().StringThatsRequired);
        }


        [TestMethod]
        public void TestExceptionBehaviorWithExceptionPropagation()
        {
            var ret = Args.ParseAction<SimpleArgs>();
            Assert.IsTrue(ret.HandledException is ArgException);
        }

        [TestMethod]
        public void TestActionFrameworkV2ExceptionHandlingErrorPath()
        {
            Parsed.Clear();
            var invoked = Args.InvokeAction<SourceControlArgs>();
            Assert.IsInstanceOfType(invoked.HandledException, typeof(MissingArgException));
            Assert.AreEqual(0, Parsed.Count);
        }


        [TestMethod]
        public void TestActionFrameworkV2ExceptionHandlingHappyPath()
        {
            Parsed.Clear();
            var invoked = Args.InvokeAction<SourceControlArgs>("push", "github", "master");

            Assert.IsNull(invoked.HandledException);
            Assert.AreEqual("Push", invoked.ActionArgsProperty.Name);

            Assert.AreEqual("github", ((PushPullArgs)invoked.ActionArgs).Remote);
            Assert.AreEqual("master", ((PushPullArgs)invoked.ActionArgs).Branch);

            Assert.AreEqual(1, Parsed.Count);
            Assert.AreEqual("github", Parsed.First().Remote);
            Assert.AreEqual("master", Parsed.First().Branch);
        }
    }
}
