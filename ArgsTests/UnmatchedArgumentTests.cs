using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class UnmatchedArgumentTests
    {
        public class MyArgs
        {
            public string FileName { get; set; }

            public bool Debug { get; set; }    
        }

        public class MyOtherArgs
        {
            public string Option { get; set; }

            public bool Flag { get; set; }
        }

        [TestMethod]
        public void TestUnmatchedArgumentsAreReturned()
        {
            var args = new List<string>
            {
                "-filename", "C:\\file.txt",
                "-flag", "-debug",
                "-option", "this is an option"
            }.ToArray();

            string[] unmatched;
            var myArgs = Args.Parse<MyArgs>(args, out unmatched);

            Assert.IsTrue(myArgs.Debug);
            Assert.IsNotNull(myArgs.FileName);

            Assert.IsTrue(unmatched.Length == 3);
        }

        [TestMethod]
        public void TestUnmatchedArgumentsCanBeProcessedSecondTime()
        {
            var args = new List<string>
            {
                "-filename", "C:\\file.txt",
                "-flag", "-debug",
                "-option", "this is an option"
            }.ToArray();

            string[] unmatched;
            Args.Parse<MyArgs>(args, out unmatched);

            var myOtherArgs = Args.Parse<MyOtherArgs>(unmatched, out unmatched);

            Assert.IsTrue(myOtherArgs.Flag);
            Assert.IsNotNull(myOtherArgs.Option);

            Assert.IsTrue(unmatched.Length == 0);
        }
    }
}