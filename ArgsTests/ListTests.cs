using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
namespace ArgsTests
{
    public class ListArgs
    {
        public List<string> Files { get; set; }
    }

    [TestClass]
    public class ListTests
    {
        [TestMethod]
        public void TestLists()
        {
            var parsed = Args.Parse<ListArgs>("-f",@"C:\test1.xml, test2.xml, test3.xml");

        }
    }
}
