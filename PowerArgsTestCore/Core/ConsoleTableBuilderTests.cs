using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class ConsoleTableBuilderTests
    {
        [TestMethod]
        public void ConsoleTableBasic()
        {
            ConsoleTableBuilder builder = new ConsoleTableBuilder();

            var columns = new List<ConsoleString>() { new ConsoleString("NAME"), new ConsoleString("ADDRESS"), new ConsoleString("DESCRIPTION") };
            var rows = new List<List<ConsoleString>>()
            {
                new List<ConsoleString>(){ new ConsoleString("Adam Abdelhamed"), new ConsoleString("One Microsoft Way, Redmond WA 98052"), new ConsoleString("The author of PowerArgs, the world's greatest command line parser and invoker.  Hooray for PowerArgs!  We love PowerArgs so much cuz it is awesome and great.  Yay!!!!  In fact, PowerArgs is so fun that nothing in the entire universe is more fun.  ") },
                new List<ConsoleString>(){ new ConsoleString("Alicia Abdelhamed"), new ConsoleString("Two Microsoft Way, Redmond WA 98052"), new ConsoleString("The wife of the author of PowerArgs, the world's greatest command line parser and invoker.  Hooray for PowerArgs!") },
            };


            var output = builder.FormatAsTable(columns, rows);
            var outstr = output.ToString();

            var expected =
@"
NAME                ADDRESS                               DESCRIPTION                                                                                                                                                                                                                                               
Adam Abdelhamed     One Microsoft Way, Redmond WA 98052   The author of PowerArgs, the world's greatest command line parser and invoker.  Hooray for PowerArgs!  We love PowerArgs so much cuz it is awesome and great.  Yay!!!!  In fact, PowerArgs is so fun that nothing in the entire universe is more fun.     
Alicia Abdelhamed   Two Microsoft Way, Redmond WA 98052   The wife of the author of PowerArgs, the world's greatest command line parser and invoker.  Hooray for PowerArgs!                                                                                                                                         
".TrimStart();

            Helpers.AssertAreEqualWithDiffInfo(expected.Replace("\r\n", "\n"), outstr.Replace("\r\n", "\n"));
        }

        [TestMethod]
        public void ConsoleTableTestMultiOptions()
        {
            ConsoleProvider.Current.BufferWidth = 160;
            ConsoleTableBuilder builder = new ConsoleTableBuilder();

            var columns = new List<ConsoleString>() { new ConsoleString("NAME"), new ConsoleString("ADDRESS"), new ConsoleString("DESCRIPTION") };
            var rows = new List<List<ConsoleString>>()
            {
                new List<ConsoleString>(){ new ConsoleString("Adam Abdelhamed"), new ConsoleString("One Microsoft Way, Redmond WA 98052"), new ConsoleString("The author of PowerArgs, the world's greatest command line parser and invoker.  Hooray for PowerArgs!  We love PowerArgs so much cuz it is awesome and great.  Yay!!!!  In fact, PowerArgs is so fun that nothing in the entire universe is more fun.  ") },
                new List<ConsoleString>(){ new ConsoleString("Alicia Abdelhamed"), new ConsoleString("Two Microsoft Way, Redmond WA 98052"), new ConsoleString("The wife of the author of PowerArgs, the world's greatest command line parser and invoker.  Hooray for PowerArgs!") },
            };

            var columnOverflowBehaviors = new List<ColumnOverflowBehavior>()
            {
                new TruncateOverflowBehavior(){ColumnWidth = 7},
                new SmartWrapOverflowBehavior(){DefineMaxWidthBasedOnConsoleWidth = false, MaxWidthBeforeWrapping = 15},
                new SmartWrapOverflowBehavior(),
            };


            var output = builder.FormatAsTable(columns, rows, rowPrefix: "", columnOverflowBehaviors: columnOverflowBehaviors);
            var outstr = output.ToString();

            var expected = 
@"
NAME         ADDRESS           DESCRIPTION
Adam...      One Microsoft     The author of PowerArgs, the world's greatest command line parser and invoker.  Hooray for PowerArgs!  We love PowerArgs so
             Way, Redmond      much cuz it is awesome and great.  Yay!!!!  In fact, PowerArgs is so fun that nothing in the entire universe is more fun.  
             WA 98052          
Alic...      Two Microsoft     The wife of the author of PowerArgs, the world's greatest command line parser and invoker.  Hooray for PowerArgs!
             Way, Redmond      
             WA 98052".TrimStart();


            Helpers.AssertAreEqualWithDiffInfo(expected.Replace("\r\n", "\n"), outstr.Replace("\r\n", "\n"));
        }

        [TestMethod]
        public void ConsoleTableTestFromExpressionBasic()
        {
            var objects = new object[]
            {
                new{ FirstName = "Adam", LastName = "Abdelhamed" },
                new{ FirstName = "John", LastName = "Doe" },
            };

            var table = new ConsoleTableBuilder().FormatAsTable(objects);
            table.WriteLine();

            Assert.IsTrue(table.Contains("FirstName"));
            Assert.IsTrue(table.Contains("LastName"));
            Assert.IsTrue(table.Contains("Adam"));
            Assert.IsTrue(table.Contains("Abdelhamed"));
            Assert.IsTrue(table.Contains("John"));
            Assert.IsTrue(table.Contains("Doe"));
        }

        [TestMethod]
        public void ConsoleTableTestFromExpressionManual()
        {
            var objects = new object[]
            {
                new{ FirstName = "Adam", LastName = "Abdelhamed" },
                new{ FirstName = "John", LastName = "Doe" },
            };

            var table = new ConsoleTableBuilder().FormatAsTable(objects, "FirstName LastName");
            table.WriteLine();

            Assert.IsTrue(table.Contains("FirstName"));
            Assert.IsTrue(table.Contains("LastName"));
            Assert.IsTrue(table.Contains("Adam"));
            Assert.IsTrue(table.Contains("Abdelhamed"));
            Assert.IsTrue(table.Contains("John"));
            Assert.IsTrue(table.Contains("Doe"));
        }


        [TestMethod]
        public void ConsoleTableTestFromExpressionOptions()
        {
            var objects = new object[]
            {
                new{ FirstName = "Adam", LastName = "Abdelhamed" },
                new{ FirstName = "John", LastName = "Doe" },
            };

            var table = new ConsoleTableBuilder().FormatAsTable(objects, "FirstName>First LastName>Last+");
            table.WriteLine();

            Assert.IsFalse(table.Contains("FirstName"));
            Assert.IsFalse(table.Contains("LastName"));

            Assert.IsTrue(table.Contains("Adam"));
            Assert.IsTrue(table.Contains("Abdelhamed"));
            Assert.IsTrue(table.Contains("John"));
            Assert.IsTrue(table.Contains("Doe"));
        }

        [TestMethod]
        public void ConsoleTableTestLongWord()
        {
            ConsoleProvider.Current.BufferWidth = 160;
            ConsoleTableBuilder builder = new ConsoleTableBuilder();

            var columns = new List<ConsoleString>() { new ConsoleString("Word 1"), new ConsoleString("Word 2") };
            var rows = new List<List<ConsoleString>>()
            {
                new List<ConsoleString>(){new ConsoleString(" ThisIsAVeryLongWord"), new ConsoleString("Hello. ThisIsAVeryLongWord ThisIsAnother AnotherVeryLongWord")}
            };

            var columnOverflowBehaviors = new List<ColumnOverflowBehavior>()
            {
                new SmartWrapOverflowBehavior(){DefineMaxWidthBasedOnConsoleWidth = false, MaxWidthBeforeWrapping = 10},
                new SmartWrapOverflowBehavior(){DefineMaxWidthBasedOnConsoleWidth = false, MaxWidthBeforeWrapping = 10}
            };

            builder.FormatAsTable(columns, rows, rowPrefix: "", columnOverflowBehaviors: columnOverflowBehaviors);
        }

        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(1, 1)]
        [DataRow(18, 1)]
        [DataRow(19, 1)]
        [DataRow(20, 1)]
        [DataRow(40, 21)]
        public void ConsoleTableTestZeroOrNarrowBufferWidth(int bufferWidth, int expectedWrapWidth)
        {
            // Issue #168: zero/negative available width must not leave the wrapping loop without progress.
            var originalWidth = ConsoleProvider.Current.BufferWidth;
            try
            {
                ConsoleProvider.Current.BufferWidth = bufferWidth;
                var columns = new List<ConsoleString> { new ConsoleString("NAME"), new ConsoleString("DESCRIPTION") };
                var rows = new List<List<ConsoleString>>
                {
                    new List<ConsoleString> { new ConsoleString("Test"), new ConsoleString("A test description that would cause wrapping") },
                };
                var wrap = new SmartWrapOverflowBehavior();
                var behaviors = new List<ColumnOverflowBehavior> { new TruncateOverflowBehavior { ColumnWidth = 10 }, wrap };

                var output = new ConsoleTableBuilder().FormatAsTable(columns, rows, columnOverflowBehaviors: behaviors);

                Assert.AreEqual(expectedWrapWidth, wrap.MaxWidthBeforeWrapping);
                Assert.AreEqual("NAMEDESCRIPTIONTestAtestdescriptionthatwouldcausewrapping", System.Text.RegularExpressions.Regex.Replace(output.ToString(), @"\s", ""));
                Assert.IsTrue(output.Length < 2000, "Wrapping should produce bounded output.");
            }
            finally
            {
                ConsoleProvider.Current.BufferWidth = originalWidth;
            }
        }

        [TestMethod]
        [DataRow(-10, 1)]
        [DataRow(0, 1)]
        [DataRow(1, 1)]
        [DataRow(8, 8)]
        public void ConsoleTableTestExplicitWrapWidth(int configuredWidth, int expectedWrapWidth)
        {
            var wrap = new SmartWrapOverflowBehavior { DefineMaxWidthBasedOnConsoleWidth = false, MaxWidthBeforeWrapping = configuredWidth };
            var columns = new List<ConsoleString> { new ConsoleString("D") };
            var rows = new List<List<ConsoleString>> { new List<ConsoleString> { new ConsoleString("Alpha beta gamma") } };
            var output = new ConsoleTableBuilder().FormatAsTable(columns, rows, columnOverflowBehaviors: new List<ColumnOverflowBehavior> { wrap });

            Assert.AreEqual(expectedWrapWidth, wrap.MaxWidthBeforeWrapping);
            Assert.AreEqual("DAlphabetagamma", System.Text.RegularExpressions.Regex.Replace(output.ToString(), @"\s", ""));
            Assert.IsTrue(output.Length < 200, "Wrapping should produce bounded output.");
        }
    }
}