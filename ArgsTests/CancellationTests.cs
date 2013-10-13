using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{


    [TestClass]
    public class CancellationTests
    {
        [CancelBeforeActionHook]
        public class Git
        {
            public string Branch { get; set; }

            [ArgActionMethod]
            public void Push()
            {
                throw new AssertFailedException("This should have been cancelled");
            }
        }

        public class CancelBeforeActionHook : ArgHook
        {
            public static bool AfterCancelCalled;

            public override void BeforeInvoke(ArgHook.HookContext context)
            {
                context.CancelAllProcessing();
            }

            public override void AfterCancel(ArgHook.HookContext context)
            {
                AfterCancelCalled = true;
            }
        }

        [TestMethod]
        public void TestCanelAction()
        {
            CancelBeforeActionHook.AfterCancelCalled = false;
            var ret = Args.InvokeAction<Git>("push", "-b", "master");
            Assert.IsNull(ret.Args);
            Assert.IsTrue(ret.Cancelled);
            Assert.IsTrue(CancelBeforeActionHook.AfterCancelCalled);
        }
    }
}
