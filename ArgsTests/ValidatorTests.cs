using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class ValidatorTests
    {
        public class CopyArgs
        {
            [ArgPosition(0)]
            [ArgRequired]
            [ArgExistingFile]
            public string From { get; set; }

            [ArgPosition(1)]
            [ArgRequired]
            [ArgExistingDirectory]
            public string To { get; set; }

            [ArgRange(0,100)]
            public int Start { get; set; }
            [ArgRange(0, 100)]
            public int End { get; set; }
        }

        [TestMethod]
        public void TestExistingFileValidatorNegative()
        {
            var invalidFileName = "notarealfile";
            var args = new string[] { invalidFileName, "C:\\windows" };
            try
            {
                var parsed = Args.ParseAction<CopyArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException e)
            {
                var inner = e.InnerException as FileNotFoundException;
                Assert.IsNotNull(inner);
                Assert.IsTrue(e.ToString().Contains(invalidFileName), "Exception message did not contain the invalid file name");
            }
        }

        [TestMethod]
        public void TestExistingFolderValidatorNegative()
        {
            var invalidFolderName = "notARealFolder";
            var args = new string[] { Path.GetTempFileName(), invalidFolderName };
            try
            {
                var parsed = Args.ParseAction<CopyArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException e)
            {
                var inner = e.InnerException as DirectoryNotFoundException;
                Assert.IsNotNull(inner);
                Assert.IsTrue(e.ToString().Contains(invalidFolderName), "Exception message did not contain the invalid directory name");
            }
        }

        [TestMethod]
        public void TestRequiredValidatorNegative()
        {
            var args = new string[] { };
            try
            {
                var parsed = Args.ParseAction<CopyArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException e)
            {
                Assert.IsTrue(e.ToString().ToLower().Contains("required"), "Arg should have been required");
            }
        }


        [TestMethod]
        public void TestRangeValidatorNegative()
        {
            double start = 0, end = 10000;
            var args = new string[] { Path.GetTempFileName(), "C:\\Windows", "-start", start + "", "-end", end + "" };

            try
            {
                var parsed = Args.ParseAction<CopyArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException ex)
            {

            }
        }

        ///////////////////////// Positive

        [TestMethod]
        public void TestExistingFileAndFolderAndRequiredValidator()
        {
            var args = new string[] { Path.GetTempFileName(), "C:\\windows" };
            var parsed = Args.ParseAction<CopyArgs>(args);
        }

        [TestMethod]
        public void TestRangeValidator()
        {
            double start = 0, end = 10;
            var args = new string[] { Path.GetTempFileName(), "C:\\Windows", "-start", start + "", "-end", end + "" };
            var parsed = Args.ParseAction<CopyArgs>(args);
        }
    }
}
