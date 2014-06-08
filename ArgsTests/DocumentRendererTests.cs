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
            Assert.AreEqual("Hi", rendered);
        }

        [TestMethod]
        public void TestDocumentRenderingWithSimpleReplacement()
        {
            var rendered = DocumentRenderer.Render("Hi {{Name!}}!", new { Name = "Adam" });
            Assert.AreEqual("Hi Adam!", rendered);
        }

        [TestMethod]
        public void TestDocumentRenderingWithConditionalReplacement()
        {
            var rendered = DocumentRenderer.Render("{{if GoodMood}}Hi {{Name!}}!{{if}}", new { Name = "Adam", GoodMood = true });
            Assert.AreEqual("Hi Adam", rendered);

            rendered = DocumentRenderer.Render("{{if GoodMood}}Hi {{Name!}}!{{if}}", new { Name = "Adam", GoodMood = false });
            Assert.AreEqual("", rendered);
        }

        [TestMethod]
        public void TestDocumentRenderingWithEachReplacement()
        {
            var rendered = DocumentRenderer.Render("{{each number in Numbers}}{{number!}}!{{each}}", new { Numbers = new int[] { 1, 2, 3, 4 } });
            Assert.AreEqual("1234", rendered);
        }
    }
}
