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

        [StickyArgPersistence(typeof(SampleStickyArgs2))]
        public class SampleStickyArgs2 : IStickyArgPersistenceProvider
        {
            public static int SaveCount, LoadCount;

            [StickyArg]
            public int Sticky { get; set; }

            public void Save(Dictionary<string, string> stickyArgs, string pathInfo)
            {
                SaveCount++;
            }

            public Dictionary<string, string> Load(string pathInfo)
            {
                LoadCount++;
                return new Dictionary<string, string> { { "Sticky", "999"}, };
            }
        }

        [StickyArgPersistence(typeof(SampleStickyArgsInvalid))] // This type does not correctly implement the interface it needs to.
        public class SampleStickyArgsInvalid
        {
            public static int SaveCount, LoadCount;
            [StickyArg]
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

        [TestMethod]
        public void TestStickyArgsWithCustomPersistence()
        {
            int saveCount = SampleStickyArgs2.SaveCount, loadCount = SampleStickyArgs2.LoadCount;
            var args = new string[] { };
            var parsed = Args.Parse<SampleStickyArgs2>(args);
            Assert.AreEqual(999, parsed.Sticky);
            Assert.IsTrue(SampleStickyArgs2.SaveCount > saveCount);
            Assert.IsTrue(SampleStickyArgs2.LoadCount > loadCount);
        }

        [TestMethod]
        public void TestStickyArgsWithCustomPersistenceButInvalidType()
        {
            try
            {
                var args = new string[] { };
                var parsed = Args.Parse<SampleStickyArgsInvalid>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (InvalidArgDefinitionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("IStickyArgPersistenceProvider"));
            }
        }
    }
}
