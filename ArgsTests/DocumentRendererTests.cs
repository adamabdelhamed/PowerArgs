using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
namespace ArgsTests
{
    [TestClass]
    public class DocumentRendererTests
    {
        [TestMethod]
        public void TestDocumentRenderingWithNoReplacements()
        {
            var rendered = DocumentRenderer.Render("Hi", 1);
            Assert.AreEqual("Hi", rendered.ToString());
        }

        [TestMethod]
        public void TestDocumentRenderingWithSimpleReplacement()
        {
            var rendered = DocumentRenderer.Render("Hi {{Name Red!}}! Sup?", new { Name = "Adam" });
            Assert.AreEqual("Hi Adam! Sup?", rendered.ToString());
            Assert.AreEqual(new ConsoleCharacter('a').ForegroundColor, rendered[0].ForegroundColor);
            Assert.AreEqual(ConsoleColor.Red, rendered[3].ForegroundColor);
            Assert.AreEqual(new ConsoleCharacter('a').ForegroundColor, rendered[rendered.Length - 1].ForegroundColor);
        }

        [TestMethod]
        public void TestDocumentRenderingWithConditionalReplacement()
        {
            var rendered = DocumentRenderer.Render("{{if GoodMood}}Hi {{Name!}}!{{if}}", new { Name = "Adam", GoodMood = true });
            Assert.AreEqual("Hi Adam", rendered.ToString());

            rendered = DocumentRenderer.Render("{{if GoodMood}}Hi {{Name!}}!{{if}}", new { Name = "Adam", GoodMood = false });
            Assert.AreEqual("", rendered.ToString());
        }

        [TestMethod]
        public void TestDocumentRenderingWithEachReplacement()
        {
            var rendered = DocumentRenderer.Render("{{each number in Numbers}}{{number!}}!{{each}}", new { Numbers = new int[] { 1, 2, 3, 4 } });
            Assert.AreEqual("1234", rendered.ToString());
        }

        class TestArgs
        {
            public string StringArg { get; set; }
            public string IntArg { get; set; }
        }

        [TestMethod]
        public void TestUsagePrimitive()
        {
            CommandLineArgumentsDefinition def = new CommandLineArgumentsDefinition(typeof(TestArgs));
            var rendered = DocumentRenderer.Render("Your program has {{Arguments.Count!}} arguments", def);
            Assert.AreEqual("Your program has 2 arguments", rendered.ToString());
        }
    }
}
