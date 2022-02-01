using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgsTests
{
    public static class TestRecorder
    {
        public static ConsoleBitmapVideoWriter CreateTestRecorder(string testName, TestContext context)
        {
            var outputDir = @"C:\temp\recordings";

            if(Directory.Exists(outputDir) == false)
            {
                Directory.CreateDirectory(outputDir);
            }

            var outputFile = Path.Combine(outputDir, testName + ".vid");

            return new ConsoleBitmapVideoWriter(s => File.WriteAllText(outputFile, s));
        }
    }
}
