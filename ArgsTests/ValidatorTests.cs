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

            [ArgRange(0, 100)]
            public int Start { get; set; }
            [ArgRange(0, 100)]
            public int End { get; set; }

            [ArgRange(0, 100, MaxIsExclusive = true)]
            public int SomeNumber { get; set; }
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
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ValidationArgException));
                Assert.AreEqual("File not found - notarealfile", ex.Message);

                Assert.IsNotNull(ex.InnerException, "Missing inner exception");
                Assert.IsInstanceOfType(ex.InnerException, typeof(FileNotFoundException));
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
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ValidationArgException));
                Assert.AreEqual("Directory not found: 'notARealFolder'", ex.Message);

                Assert.IsNotNull(ex.InnerException, "Missing inner exception");
                Assert.IsInstanceOfType(ex.InnerException, typeof(DirectoryNotFoundException));
            }
        }
        
        [TestMethod]
        public void ArgRequiredValidatorThrowsOnMissingArg()
        {
            var args = new string[] { };

            try
            {
                var parsed = Args.ParseAction<CopyArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(MissingArgException));
                Assert.AreEqual("The argument 'From' is required", ex.Message);
            }
        }

        [TestMethod]
        public void ArgRangeThrowsOnNonNumericValue()
        {
            try
            {
                var args = new string[] { Path.GetTempFileName(), "C:\\Windows", "-start", "ABC" };
                Args.Parse<CopyArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ValidationArgException));
                Assert.AreEqual("Expected a number for arg: Start", ex.Message);
            }
        }

        [TestMethod]
        public void ArgRangeThrowsOnValueGreaterThanMaxInclusive()
        {
            try
            {
                var args = new string[] { Path.GetTempFileName(), "C:\\Windows", "-start", 101 + "" };
                Args.Parse<CopyArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ValidationArgException));
                Assert.AreEqual("Start must be at least 0, but not greater than 100", ex.Message);
            }
        }

        [TestMethod]
        public void ArgRangeThrowsOnValueEqualToMaxExclusive()
        {
            try
            {
                var args = new string[] { Path.GetTempFileName(), "C:\\Windows", "-somenumber", 100 + "" };
                Args.Parse<CopyArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ValidationArgException));
                Assert.AreEqual("SomeNumber must be at least 0, and less than 100", ex.Message);
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
        public void TestRangeValidatorMaxInclusive()
        {
            var correctValue = 100;
            var correctArgs = new string[] { Path.GetTempFileName(), "C:\\Windows", "-start", correctValue + "" };
            var parsedShouldwork = Args.Parse<CopyArgs>(correctArgs);
            Assert.AreEqual(correctValue, parsedShouldwork.Start);
        }

        [TestMethod]
        public void TestRangeValidatorMaxExclusive()
        {
            var correctValue = 99;
            var correctArgs = new string[] { Path.GetTempFileName(), "C:\\Windows", "-somenumber", correctValue + "" };
            var parsedShouldwork = Args.Parse<CopyArgs>(correctArgs);
            Assert.AreEqual(correctValue, parsedShouldwork.SomeNumber);
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
            catch (Exception ex)
            {
                if (expectValid) Assert.Fail(input + " should have been valid");
                
                Assert.IsInstanceOfType(ex, typeof(ValidationArgException));
                Assert.AreEqual("Invalid social security number: " + input, ex.Message);
            }
        }
    }
}
