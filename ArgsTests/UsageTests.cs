using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace ArgsTests
{
    public class UsageAutomation : Attribute {}

    public class BasicUsageArgs
    {
        [ArgPosition(0)]
        [ArgDescription("A string arg")]
        public string StringArgs { get; set; }
        [ArgDescription("An int arg")]
        public int IntArgs { get; set; }
    }

    public class ArgsForActionSpecificUsage
    {
        [ArgActionMethod]
        public void Foo([ArgDescription("The only param")]string param) 
        {
            throw new ArgException("We want to show you the Foo specific help");
        }

        [ArgActionMethod]
        public void Bar() { }
    }

    [TestClass]
    public class UsageTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestAllUsageOutputs()
        {
            var outputDataFile = @"C:\temp\PowerArgsUsageTestOutput.txt";
            string s = "";

            try
            {
                if (Directory.Exists(Path.GetDirectoryName(outputDataFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputDataFile));
                }

                Dictionary<string, ArgUsageOptions> optionVariations = new Dictionary<string, ArgUsageOptions>()
            {
                { "Default" , new ArgUsageOptions() },
                { "Minimal", new ArgUsageOptions(){ ShowPosition = false, ShowType = false } },
            };


                foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttributes(typeof(UsageAutomation), true).Length > 0))
                {
                    s+= (type.FullName + "\n\n");
                    foreach (var variation in optionVariations.Keys)
                    {
                        s += (variation + "\n\n");
                        s += ("" + ArgUsage.GetStyledUsage(type, "testusage.exe", options: optionVariations[variation]));
                        s += "\n\n";
                    }
                }

            }
            finally
            {
                File.WriteAllText(outputDataFile, s.Replace("\n","\r\n"), Encoding.UTF8);
            }
        }

        [TestMethod]
        public void TestActionSpecificUsage()
        {
            try
            {
                Args.InvokeAction<ArgsForActionSpecificUsage>("Foo");
            }
            catch (ArgException ex)
            {
                var usage = ArgUsage.GetStyledUsage<ArgsForActionSpecificUsage>("test", new ArgUsageOptions()
                {
                    SpecifiedActionOverride = ex.Context.SpecifiedAction
                });
                Assert.IsTrue(usage.Contains("Foo"));
                Assert.IsFalse(usage.Contains("Bar"));
            }
        }

        [TestMethod]
        public void TestUsageWithoutTypeAndPosition()
        {
            var usage = ArgUsage.GetUsage<BasicUsageArgs>("test", new ArgUsageOptions() 
            {
                ShowType = false,
                ShowPosition=false,
            });

            Assert.IsFalse(usage.Contains("TYPE"));
            Assert.IsFalse(usage.Contains("POSITION"));
        }

        [TestMethod]
        public void TestUsageWithTypeAndPosition()
        {
            var usage = ArgUsage.GetUsage<BasicUsageArgs>("test");

            Assert.IsTrue(usage.Contains("TYPE"));
            Assert.IsTrue(usage.Contains("POSITION"));
        }

        [TestMethod]
        public void TestUsageWithTypeAndNotPosition()
        {
            var usage = ArgUsage.GetUsage<BasicUsageArgs>("test", new ArgUsageOptions()
            {
                ShowType = true,
                ShowPosition = false,
            });

            Assert.IsTrue(usage.Contains("TYPE"));
            Assert.IsFalse(usage.Contains("POSITION"));
        }


        [TestMethod]
        public void TestUsageWithNoTypeAndWithPosition()
        {
            var usage = ArgUsage.GetUsage<BasicUsageArgs>("test", new ArgUsageOptions()
            {
                ShowType = false,
                ShowPosition = true,
            });

            Assert.IsFalse(usage.Contains("TYPE"));
            Assert.IsTrue(usage.Contains("POSITION"));
        }

        #region Samples

        [UsageAutomation]
        public class GitArgs
        {
            public class PushPullArgs
            {
                [ArgPosition(1), ArgRequired, ArgShortcut(ArgShortcutPolicy.NoShortcut), ArgDescription("The remote repo")]
                public string Remote { get; set; }

                [ArgPosition(2), ArgRequired, ArgShortcut(ArgShortcutPolicy.NoShortcut), DefaultValue("master"), ArgDescription("The branch to use")]
                public string Branch { get; set; }
            }

            public class CommitArgs
            {
                [ArgShortcut(ArgShortcutPolicy.ShortcutsOnly), ArgShortcut("-a"), ArgShortcut("--all"),ArgDescription("Tell the command to automatically stage files that have been modified and deleted, but new files you have not told git about are not affected.")]
                public bool All { get; set; }

                [ArgShortcut(ArgShortcutPolicy.ShortcutsOnly), ArgShortcut("-p"), ArgShortcut("--patch"), ArgDescription("Use the interactive patch selection interface to chose which changes to commit. See git-add(1) for details.")]
                public bool Patch { get; set; }

                [ArgShortcut(ArgShortcutPolicy.ShortcutsOnly), ArgShortcut("--dry-run"), ArgDescription("Do not create a commit, but show a list of paths that are to be committed, paths with local changes that will be left uncommitted and paths that are untracked.")]
                public bool DryRun { get; set; }
            }

            [ArgActionMethod, ArgDescription("Push local changes to a remore repository")]
            public void Push(PushPullArgs args) {}

            [ArgActionMethod, ArgDescription("Pull local changes from a remore repository")]
            public void Pull(PushPullArgs args) { }

            [ArgActionMethod, ArgDescription("Commit local changes to the local repository")]
            public void Commit(CommitArgs args) { }
        }



        #endregion

    }
}
