using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Linq;

namespace PowerArgsTestCore.Core
{
    public class WhichShortcutHook : ArgHook
    {
        public string ArgVal { get; set; }
        public override void AfterPopulateProperty(HookContext context)
        {
            ArgVal = context.CmdLineArgs.Where(a => a.ToLower() == "-v" || a.ToLower() == "-vv").Single();
        }
    }

    [TestClass]
    public class WhichShortcutTest
    {

        [ArgShortcut("-v")]
        [ArgShortcut("-vv")]
        [WhichShortcutHook]
        public bool Verbose { get; set; }


        [TestMethod]
        public void TestWhichShortcut()
        {
            var def = new CommandLineArgumentsDefinition(typeof(WhichShortcutTest));
            var parsed = Args.Parse(def,"-vv");
            var which = def.Arguments
                .Where(a => a.DefaultAlias == nameof(Verbose))
                .Single().Metadata
                .WhereAs<WhichShortcutHook>().Single().ArgVal;
            Assert.AreEqual("-vv", which);
        }
    }
}
