using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
namespace ArgsTests
{
    public class ListArgs
    {
        public bool SomeBool { get; set; }

        [ArgPosition(0)]
        public List<string> Files { get; set; }
    }

    [TestClass]
    [TestCategory(Categories.Core)]
    public class ListTests
    {
        [TestMethod]
        public void TestLists()
        {
            var parsed = Args.Parse<ListArgs>("-f",@"C:\test1.xml, test2.xml, test3.xml");

        }

        [TestMethod]
        public void TestListsNewSyntax()
        {
            var parsed = Args.Parse<ListArgs>("-f", "C:\test1.xml", "test2.xml", "test3.xml", "-somebool");
            Assert.AreEqual(true, parsed.SomeBool);
            Assert.AreEqual(3, parsed.Files.Count);

            parsed = Args.Parse<ListArgs>("-somebool", "-f", "C:\test1.xml", "test2.xml", "test3.xml");
            Assert.AreEqual(true, parsed.SomeBool);
            Assert.AreEqual(3, parsed.Files.Count);

            parsed = Args.Parse<ListArgs>("-f", "C:\test1.xml", "test2.xml", "test3.xml");
            Assert.AreEqual(false, parsed.SomeBool);
            Assert.AreEqual(3, parsed.Files.Count);
        }

        [TestMethod]
        public void TestListsNewSyntaxByPosition()
        {
            var parsed = Args.Parse<ListArgs>("C:\test1.xml", "test2.xml", "test3.xml", "-somebool");
            Assert.AreEqual(true, parsed.SomeBool);
            Assert.AreEqual(3, parsed.Files.Count);

            parsed = Args.Parse<ListArgs>("C:\test1.xml", "test2.xml", "test3.xml");
            Assert.AreEqual(false, parsed.SomeBool);
            Assert.AreEqual(3, parsed.Files.Count);
        }
    }
}
