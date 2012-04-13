using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class StickyArgTests
    {
        public class SampleArgs
        {
            public int NotSticky { get; set; }
            [StickyArg]
            [ArgRequired]
            public int Sticky { get; set; }
        }

        [TestMethod]
        public void TestStickyArgs()
        {
            var args = new string[] { "-s", "12345" };
            Args.Parse<SampleArgs>(args);
            var remembered = Args.Parse<SampleArgs>(new string[0]);
            Assert.AreEqual(12345, remembered.Sticky);
        }
    }
}
