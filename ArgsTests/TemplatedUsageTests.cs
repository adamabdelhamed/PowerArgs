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

            AssertAreEqualWithDiffInfo(Resources.PhotoAlbumManagerExpectedBrowserUsage.Replace("\r\n","\n"), browserUsage);
            AssertAreEqualWithDiffInfo(Resources.PhotoAlbumManagerExpectedConsoleUsage.Replace("\r\n", "\n"), consoleUsage);
        }

        private static void AssertAreEqualWithDiffInfo(string expected, string actual)
        {
            int verified = 0;

            int line = 1, col = 1;

            for(int i = 0; i < expected.Length;i++)
            {
                if(i > actual.Length - 1)
                {
                    break;
                }

                var expectedChar = expected[i];
                var actualChar = actual[i];
                if (expectedChar != actualChar)
                {
                    Assert.Fail("Character on line " + line + " and col " + col + " did not match.  Expected '" + expectedChar + ", actual '" +actualChar + "'");
                }

                verified++;

                if (expected[i] == '\n')
                {
                    line++;
                    col = 1;
                }
                else
                {
                    col++;
                }
            }

            if(verified != expected.Length)
            {
                Assert.Fail("Verified " + verified + " characters, expected " + expected.Length + " characters");
            }
        }
    }
}
