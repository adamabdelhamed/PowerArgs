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

        public class PhoneNumberArgs
        {
            public USPhoneNumber PhoneNumber { get; set; }
        }

        public class RegexArgs
        {
            [ArgRegex(@"\d{3}-\d{2}-\d{4}", "Invalid social security number")]
            public string SSN { get; set; }
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

        [TestMethod]
        public void TestValidSSN()
        {
            TestSSN("111-22-3333");
        }

        [TestMethod]
        public void TestInvalidSSN()
        {
            TestSSN("111223333", false);    // Missing dashes
            TestSSN("111-222-3333", false); // Too many numbers
            TestSSN("aaa-22-3333", false);  // Letters
            TestSSN("111-aa-3333", false);  // Letters
            TestSSN("111-22-aaaa", false);  // Letters
        }

        [TestMethod]
        public void TestValidPhoneNumbers()
        {
            TestPhoneNumber("2234567890");
            TestPhoneNumber("(223)-456-7890");
            TestPhoneNumber("223456-7890");
            TestPhoneNumber("1223-456-7890");
            TestPhoneNumber("1-223-456-7890");
        }

        [TestMethod]
        public void TestInvalidPhoneNumbers()
        {
            TestPhoneNumber("((223-456-7890", false);// extra paren
            TestPhoneNumber("(223-456-7890", false);// missing closing paren
            TestPhoneNumber("(2234567890", false);  // missing closing paren
            TestPhoneNumber("223)4567890", false);  // missing opening paren
            TestPhoneNumber("22345678900", false);  // extra number
            TestPhoneNumber("223A567890", false);  // Has a letter in the area code
            TestPhoneNumber("22345A7890", false);  // Has a letter in the first group
            TestPhoneNumber("223456789A", false);  // Has a letter in the second group
        }

        private void TestPhoneNumber(string phoneNumberInput, bool expectValid = true)
        {
            try
            {
                PhoneNumberArgs args = Args.Parse<PhoneNumberArgs>("-p", phoneNumberInput);
                if (!expectValid) Assert.Fail(phoneNumberInput + " should not have been valid");

                Assert.AreEqual("1-(223)-456-7890".Length, args.PhoneNumber.ToString().Length);
                
                Assert.AreEqual('1', args.PhoneNumber.ToString()[0]);

                Assert.AreEqual('-', args.PhoneNumber.ToString()[1]);

                Assert.AreEqual('(', args.PhoneNumber.ToString()[2]);
                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[3]) && int.Parse(args.PhoneNumber.ToString()[3] + "") > 1);
                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[4]));
                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[5]));
                Assert.AreEqual(')', args.PhoneNumber.ToString()[6]);

                Assert.AreEqual('-', args.PhoneNumber.ToString()[7]);

                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[8]));
                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[9]));
                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[10]));

                Assert.AreEqual('-', args.PhoneNumber.ToString()[11]);
                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[12]));
                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[13]));
                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[14]));
                Assert.IsTrue(char.IsDigit(args.PhoneNumber.ToString()[15]));
            }
            catch (ArgException ex)
            {
                if (expectValid) Assert.Fail(phoneNumberInput + " should have been valid");
                else Assert.IsTrue(ex.Message.Equals("Invalid phone number: " + phoneNumberInput));
            }
        }

        private void TestSSN(string input, bool expectValid = true)
        {
            try
            {
                RegexArgs args = Args.Parse<RegexArgs>("-s", input);
                if (!expectValid) Assert.Fail(input + " should not have been valid");
            }
            catch (ArgException ex)
            {
                if (expectValid) Assert.Fail(input + " should have been valid");
                else Assert.IsTrue(ex.Message.Equals("Invalid social security number: " + input));
            }
        }
    }
}
