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

    [TestClass]
    public class TemplatedUsageTests
    {
        public TestContext TestContext { get; set; }
         
        [TestMethod]
        public void TestPhotoAlbumManagerConsoleUsage()
        {
            var usage = ArgUsage.GenerateUsageFromTemplate<PhotoAlbumManagerArgs>().ToString();
        }

    }
}
