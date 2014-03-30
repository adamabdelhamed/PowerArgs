using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class TestNonInteractiveMode
    {
        [TabCompletion(REPL=true)]
        public class SomeArgs
        {
            [NonInteractiveIndicator]
            public bool NonInteractive { get; set; }

            [ArgRequired(PromptIfMissing=true)]
            public string StringArg { get; set; }
        }

        [TestMethod]
        public void TestNonInteractiveNoPrompt()
        {
            try
            {
                // the -n flag should ensure that:
                //
                //    - the PromptIfMissing flag is ignored
                //    - the REPL does not run
                //    - the TabCompletion hook does not run.
                Args.Parse<SomeArgs>("-n");
                Assert.Fail("An exception should have been thrown");
            }
            catch(MissingArgException)
            {

            }
        }
    }
}
