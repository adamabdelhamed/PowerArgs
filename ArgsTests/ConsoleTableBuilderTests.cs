using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests
{
    [TestClass]
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
            Console.BufferWidth = 160;
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
    }
}
