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
            [HelpHook(EXEName="test"), ArgShortcut("?"), ArgDescription("Displays this help")]
            public bool Help { get; set; }

            [ArgActionMethod,ArgDescription("Runs action 1, a really great action")]
            public void Action1()
            {
                throw new NotImplementedException();
            }

            [ArgActionMethod, ArgDescription("Runs action 1, a really great action")]
            public void Action2()
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void TestHelpHook()
        {
            var result = Args.InvokeAction<Command>("-?");
        }
    }
}
