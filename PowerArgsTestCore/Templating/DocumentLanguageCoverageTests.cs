using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.Templating;

[TestClass]
[TestCategory(Categories.Templating)]
public class DocumentLanguageCoverageTests
{
    [TestMethod]
    public void IfAndIfNotRenderOnlyTheirMatchingBranches()
    {
        const string template = "{{if Flag}}yes!{{if}}{{ifnot Flag}}no!{{ifnot}}";

        Assert.AreEqual("yes", new DocumentRenderer().Render(template, new { Flag = true }).ToString());
        Assert.AreEqual("no", new DocumentRenderer().Render(template, new { Flag = false }).ToString());
    }

    [TestMethod]
    public void EachExposesItemAndZeroBasedIndex()
    {
        const string template = "{{each item in Items}}{{item-index!}}={{item!}};!{{each}}";

        Assert.AreEqual("0=a;1=b;", new DocumentRenderer().Render(template, new { Items = new[] { "a", "b" } }).ToString());
    }

    [TestMethod]
    public void FalseBranchStillValidatesMalformedNestedSyntax()
    {
        const string template = "{{if Enabled}}{{each item in Items}}text!{{if}}!{{if}}";

        Assert.ThrowsExactly<DocumentRenderException>(() => new DocumentRenderer().Render(template, new { Enabled = false, Items = Array.Empty<string>() }));
    }

    [TestMethod]
    [DataRow("{{if!}}", "missing if expression")]
    [DataRow("{{if Flag Extra!}}", "unexpected parameters")]
    [DataRow("{{each!}}", "Each tag has no body")]
    [DataRow("{{each item in Items extra}}x!{{each}}", "Unexpected parameter")]
    [DataRow("{{Value Red Black extra!}}", "Unexpected parameter")]
    public void InvalidBuiltInExpressionsAreRejected(string template, string expectedMessage)
    {
        var exception = Assert.ThrowsExactly<DocumentRenderException>(() => new DocumentRenderer().Render(template, new { Flag = true, Items = Array.Empty<string>(), Value = "x" }));

        StringAssert.Contains(exception.Message, expectedMessage);
    }

    [TestMethod]
    public void MissingAndDuplicateNamedTemplatesHaveClearFailures()
    {
        var renderer = new DocumentRenderer();
        renderer.RegisterTemplate("item", new DocumentTemplateInfo { Value = "{{Name!}}" });

        Assert.ThrowsExactly<ArgumentException>(() => renderer.RegisterTemplate("item", new DocumentTemplateInfo { Value = "duplicate" }));
        Assert.ThrowsExactly<DocumentRenderException>(() => renderer.Render("{{template missing Data!}}", new { Data = new { Name = "Ada" } }));
        renderer.UnregisterTemplate("item");
        Assert.ThrowsExactly<KeyNotFoundException>(() => renderer.UnregisterTemplate("item"));
    }

    [TestMethod]
    public void ExpressionProviderRegistrationRejectsNullsAndProtectsBuiltIns()
    {
        var parser = new DocumentExpressionParser();
        var provider = new ConstantExpressionProvider();

        Assert.ThrowsExactly<ArgumentNullException>(() => parser.RegisterReplacementExpressionProvider(null!, provider));
        Assert.ThrowsExactly<ArgumentNullException>(() => parser.RegisterReplacementExpressionProvider("custom", null!));
        Assert.ThrowsExactly<ArgumentException>(() => parser.RegisterReplacementExpressionProvider("if", provider));

        parser.RegisterReplacementExpressionProvider("custom", provider);
        Assert.IsTrue(parser.RegisteredReplacementExpressionProviderKeys.Contains("custom"));
        parser.RegisterReplacementExpressionProvider("custom", new ConstantExpressionProvider(), true);
        parser.UnregisterReplacementExpressionProvider("custom");
        Assert.ThrowsExactly<ArgumentException>(() => parser.UnregisterReplacementExpressionProvider("custom"));
    }

    [TestMethod]
    public void CompiledTemplateCacheTracksExpressionProviderChanges()
    {
        var renderer = new DocumentRenderer();
        renderer.ExpressionParser.RegisterReplacementExpressionProvider("custom", new TextExpressionProvider("first"));
        Assert.AreEqual("first", renderer.Render("{{custom!}}", new { }).ToString());

        renderer.ExpressionParser.RegisterReplacementExpressionProvider("custom", new TextExpressionProvider("second"), true);
        Assert.AreEqual("second", renderer.Render("{{custom!}}", new { }).ToString());
    }

    [TestMethod]
    public void CustomProviderCreatesASeparateExpressionForEachRender()
    {
        var renderer = new DocumentRenderer();
        var provider = new CountingExpressionProvider();
        renderer.ExpressionParser.RegisterReplacementExpressionProvider("count", provider);

        renderer.Render("{{count!}}", new { });
        renderer.Render("{{count!}}", new { });

        Assert.AreEqual(2, provider.CreateCount);
    }

    [TestMethod]
    public void RendererSafelyReusesCompiledTemplateAcrossConcurrentCalls()
    {
        var renderer = new DocumentRenderer();
        Parallel.For(0, 500, i => Assert.AreEqual("value-" + i, renderer.Render("{{Value!}}", new { Value = "value-" + i }).ToString()));
    }

    private sealed class ConstantExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(DocumentExpressionContext context) => new FuncDocExpression(_ => new ConsoleString("constant"));
    }

    private sealed class TextExpressionProvider : IDocumentExpressionProvider
    {
        private readonly string text;

        public TextExpressionProvider(string text) => this.text = text;

        public IDocumentExpression CreateExpression(DocumentExpressionContext context) => new FuncDocExpression(_ => new ConsoleString(text));
    }

    private sealed class CountingExpressionProvider : IDocumentExpressionProvider
    {
        public int CreateCount { get; private set; }

        public IDocumentExpression CreateExpression(DocumentExpressionContext context)
        {
            CreateCount++;
            return new FuncDocExpression(_ => ConsoleString.Empty);
        }
    }
}
