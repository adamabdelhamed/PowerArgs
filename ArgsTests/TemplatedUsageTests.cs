using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
namespace ArgsTests
{

    [TestClass]
    public class TemplatedUsageTests
    {
        public TestContext TestContext { get; set; }
         
        [TestMethod]
        public void TestPhotoAlbumManagerConsoleUsage()
        {
            var def = new CommandLineArgumentsDefinition(typeof(PhotoAlbumManagerArgs));
            def.ExeName = "PhotoManager";
            var browserUsage = ArgUsage.GenerateUsageFromTemplate(def, template: PowerArgs.Resources.DefaultBrowserUsageTemplate).ToString().Replace("\r\n", "\n");
            var consoleUsage = ArgUsage.GenerateUsageFromTemplate(def).ToString().Replace("\r\n", "\n");

            Helpers.AssertAreEqualWithDiffInfo(Resources.PhotoAlbumManagerExpectedBrowserUsage.Replace("\r\n","\n"), browserUsage);
            Helpers.AssertAreEqualWithDiffInfo(Resources.PhotoAlbumManagerExpectedConsoleUsage.Replace("\r\n", "\n"), consoleUsage);
        }
    }
}
