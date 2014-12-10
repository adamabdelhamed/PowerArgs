using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
using System.Reflection;
namespace ArgsTests.Templating
{
    public class FuncDocExpression : IDocumentExpression
    {
        Func<DocumentRendererContext, ConsoleString> impl;
        public FuncDocExpression(Func<DocumentRendererContext,ConsoleString> impl)
        {
            this.impl = impl;
        }

        public ConsoleString Evaluate(DocumentRendererContext context)
        {
            return impl(context);
        }
    }

    [DynamicExpressionProvider("date")]
    public class DateExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(DocumentExpressionContext c)
        {
            return new FuncDocExpression((context) => { return new ConsoleString(DateTime.Now.Date.ToString()); });
        }
    }

    [DynamicExpressionProvider("if")]
    public class IfOverrideExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(DocumentExpressionContext c)
        {
            return new FuncDocExpression((context) => { return new ConsoleString("if override"); });
        }
    }

    [TestClass]
    public class DocumentRendererTests
    {
        [TestMethod]
        public void TestDocumentRenderingVarExpressionColors()
        {
            var rendered = new DocumentRenderer().Render("{{var ConsoleForegroundColor Red!}}Hi {{FirstName!}}{{clearvar ConsoleForegroundColor !}}.  How are you?", new { FirstName = "Adam" });
            Assert.AreEqual(new ConsoleString("Hi Adam",foregroundColor: ConsoleColor.Red) + new ConsoleString(".  How are you?"), rendered);
        }

        [TestMethod]
        public void TestDocumentRenderingVarExpression()
        {
            var rendered = new DocumentRenderer().Render("{{var first FirstName!}}Hi {{first!}}{{clearvar first!}}", new { FirstName="Adam" });
            Assert.AreEqual("Hi Adam", rendered.ToString());
        }

        [TestMethod]
        public void TestDocumentRenderingWithCustomExpressions()
        {
            DocumentRenderer renderer = new DocumentRenderer();
            renderer.ExpressionParser.RegisterDynamicReplacementExpressionProviders(Assembly.GetExecutingAssembly(), true);
            var rendered = renderer.Render("{{ date !}}", new { });
            Assert.AreEqual(DateTime.Today.ToString(), rendered.ToString());

            Assert.IsTrue(renderer.ExpressionParser.RegisteredReplacementExpressionProviderKeys.Contains("date"));
            renderer.ExpressionParser.UnregisterReplacementExpressionProvider("date");
            Assert.IsFalse(renderer.ExpressionParser.RegisteredReplacementExpressionProviderKeys.Contains("date"));
        }

        [TestMethod]
        public void TestDocumentRenderingWithCustomOverrideExpressions()
        {
            DocumentRenderer renderer = new DocumentRenderer();
            renderer.ExpressionParser.RegisterDynamicReplacementExpressionProviders(Assembly.GetExecutingAssembly(), true);
            var rendered = renderer.Render("{{ if !}}", new { });
            Assert.AreEqual("if override", rendered.ToString());
        }

        [TestMethod]
        public void TestDocumentRenderingWithCustomInvalidOverrideExpressions()
        {
            try
            {
                DocumentRenderer renderer = new DocumentRenderer();
                renderer.ExpressionParser.RegisterDynamicReplacementExpressionProviders(Assembly.GetExecutingAssembly(), false);
                Assert.Fail("An exception should have been thrown");
            }
            catch(ArgumentException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("already exists"));
            }
        }

        [TestMethod]
        public void TestDocumentRenderingWithNoReplacements()
        {
            var rendered = new DocumentRenderer().Render("Hi", 1);
            Assert.AreEqual("Hi", rendered.ToString());
        }

        [TestMethod]
        public void TestDocumentRenderingWithSimpleReplacement()
        {
            var rendered = new DocumentRenderer().Render("Hi {{Name Red!}}! Sup?", new { Name = "Adam" });
            Assert.AreEqual("Hi Adam! Sup?", rendered.ToString());
            Assert.AreEqual(new ConsoleCharacter('a').ForegroundColor, rendered[0].ForegroundColor);
            Assert.AreEqual(ConsoleColor.Red, rendered[3].ForegroundColor);
            Assert.AreEqual(new ConsoleCharacter('a').ForegroundColor, rendered[rendered.Length - 1].ForegroundColor);
        }

        [TestMethod]
        public void TestDocumentRenderingWithConditionalReplacement()
        {
            var rendered = new DocumentRenderer().Render("{{if GoodMood}}Hi {{Name!}}!{{if}}", new { Name = "Adam", GoodMood = true });
            Assert.AreEqual("Hi Adam", rendered.ToString());

            rendered = new DocumentRenderer().Render("{{if GoodMood}}Hi {{Name!}}!{{if}}", new { Name = "Adam", GoodMood = false });
            Assert.AreEqual("", rendered.ToString());
        }

        [TestMethod]
        public void TestDocumentRenderingWithEachReplacement()
        {
            var rendered = new DocumentRenderer().Render("{{each number in Numbers}}{{number!}}!{{each}}", new { Numbers = new int[] { 1, 2, 3, 4 } });
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
            var rendered = new DocumentRenderer().Render("Your program has {{Arguments.Count!}} arguments", def);
            Assert.AreEqual("Your program has 2 arguments", rendered.ToString());
        }

        [TestMethod]
        public void TestDocumentRenderingWithNamedTemplates()
        {
            DocumentRenderer renderer = new DocumentRenderer();
            renderer.RegisterTemplate("TheOneAndOnlyTemplate", new DocumentTemplateInfo()
            {
                Value = "Hi {{LastName!}}, {{FirstName!}}",
            });
            var rendered = renderer.Render("[{{template TheOneAndOnlyTemplate DudeData!}}]", new { DudeData = new { FirstName = "John", LastName = "Smith" } });
            Assert.AreEqual("[Hi Smith, John]", rendered.ToString());
        }


        [TestMethod]
        public void TestDocumentRenderingSourcePropagation()
        {
            DocumentRenderer renderer = new DocumentRenderer();
            renderer.RegisterTemplate("TheOneAndOnlyTemplate", new DocumentTemplateInfo()
            {
                Value = "Hi {{!}}, {{FirstName!}}",
                SourceLocation = "The bad template",
            });

            try
            {
                var rendered = renderer.Render("[{{template TheOneAndOnlyTemplate DudeData!}}]", new { DudeData = new { FirstName = "John", LastName = "Smith" } });
                Assert.Fail("An exception should have been thrown");
            }
            catch(DocumentRenderException ex)
            {
                Assert.IsTrue(ex.Message.Contains("The bad template"));
            }
        }

        [TestMethod]
        public void TestDocumentRenderingNewLinesBehvior()
        {
            var template = 
@"
{{ each foo in Foos }}

!{{ each }}
".TrimStart().Replace("\r\n","\n");
            var rendered = new DocumentRenderer().Render(template, new { Foos = new int[] { 1, 2, 3 } }).ToString();
            Assert.AreEqual("\n\n\n", rendered);
        }
    }
}
