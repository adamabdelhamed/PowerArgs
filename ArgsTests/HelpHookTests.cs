using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class HelpHookTests
    {
        public class Command
        {
            public static event Action Action1Fired;

            [HelpHook(EXEName="test"), ArgShortcut("?"), ArgDescription("Displays this help")]
            public bool Help { get; set; }

            [ArgActionMethod,ArgDescription("Runs action 1, a really great action")]
            public void Action1()
            {
                if (Action1Fired != null) Action1Fired();
            }

            [ArgActionMethod, ArgDescription("Runs action 2, a really great action")]
            public void Action2()
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void TestHelpHook()
        {
            int fireCount = 0;
            Action handler = () =>
            {
                fireCount++;
            };

            Command.Action1Fired += handler;

            var result = Args.InvokeAction<Command>("-?");
            var result2 = Args.InvokeAction<Command>("Action1");
            Assert.AreEqual(1, fireCount);
        }

        [TestMethod]
        public void TestHelpHookContextual()
        {
            try
            {
                ConsoleOutInterceptor.Instance.Attach();
                ConsoleOutInterceptor.Instance.ReadAndClear();
                var result = Args.InvokeAction<Command>("Action2", "-?");
                var output = new ConsoleString(ConsoleOutInterceptor.Instance.ReadAndClear());
                Assert.IsFalse(output.Contains("Action1", StringComparison.InvariantCultureIgnoreCase));
            }
            finally
            {
                ConsoleOutInterceptor.Instance.Detatch();
            }
        }
    }
}
