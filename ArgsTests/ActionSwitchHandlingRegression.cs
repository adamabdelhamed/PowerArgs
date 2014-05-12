using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class ActionSwitchHandlingRegression
    {
        /// <summary>
        /// Used when a user resets saved settings
        /// </summary>
        public class SettingsArgs
        {
            public string Cert { get; set; }

            public int RetryCount { get; set; }

            public int MaxConcurrentRequests { get; set; }

            public bool Reset { get; set; }
        }

        public class Definition
        {
            public static int SettingsInvokedCount = 0;

            [ArgActionMethod]
            public void Settings(SettingsArgs args)
            {
                Assert.AreEqual(5, args.RetryCount);
                Assert.IsTrue(args.Reset);
                SettingsInvokedCount++;
            }
        }

        [TestMethod]
        public void RegressActionSwitchingIssue()
        {
            Definition.SettingsInvokedCount = 0;
            Args.InvokeAction<Definition>("settings", "-retrycount", "5", "-reset");
            Assert.AreEqual(1, Definition.SettingsInvokedCount);
            Args.InvokeAction<Definition>("settings", "-reset", "-retrycount", "5");
            Assert.AreEqual(2, Definition.SettingsInvokedCount);
        }
    }
}
