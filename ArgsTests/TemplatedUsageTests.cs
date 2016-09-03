using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System;
namespace ArgsTests
{

    [TestClass]
    public class TemplatedUsageTests
    {
        public TestContext TestContext { get; set; }
         
        [TestMethod]
        public void TestPhotoAlbumManagerConsoleUsage()
        {
            ConsoleProvider.Current = new CLI.CliUnitTestConsole() { BufferWidth = 160 };
            var def = new CommandLineArgumentsDefinition(typeof(PhotoAlbumManagerArgs));
            def.ExeName = "PhotoManager";
            var browserUsage = ArgUsage.GenerateUsageFromTemplate(def, template: PowerArgs.Resources.DefaultBrowserUsageTemplate).ToString().Replace("\r\n", "\n").Replace("\r", "\n");
            var consoleUsage = ArgUsage.GenerateUsageFromTemplate(def).ToString().Replace("\r\n", "\n").Replace("\r", "\n");



            Helpers.AssertAreEqualWithDiffInfo(Resources.PhotoAlbumManagerExpectedBrowserUsage.Replace("\r\n","\n").Replace("\r", "\n"), browserUsage);
            Helpers.AssertAreEqualWithDiffInfo(Resources.PhotoAlbumManagerExpectedConsoleUsage.Replace("\r\n", "\n").Replace("\r", "\n"), consoleUsage);
        }
    }
}
