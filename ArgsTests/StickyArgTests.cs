using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.IO;

namespace ArgsTests
{
    [TestClass]
    public class StickyArgTests
    {
        const string tempFile = @"C:\temp\stickyargs.txt";

        public class SampleArgs
        {
            public int NotSticky { get; set; }
            [StickyArg(tempFile)]
            [ArgRequired]
            public int Sticky { get; set; }
        }

        [TestMethod]
        public void TestStickyArgs()
        {
            var tempDir = Path.GetDirectoryName(tempFile);
            if (Directory.Exists(tempDir) == false) Directory.CreateDirectory(tempDir);
            if (File.Exists(tempFile)) File.Delete(tempFile);

            var args = new string[] { "-s", "12345" };
            Args.Parse<SampleArgs>(args);
            var remembered = Args.Parse<SampleArgs>(new string[0]);
            Assert.AreEqual(12345, remembered.Sticky);
        }
    }
}
