using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.Templating;

[TestClass]
[TestCategory(Categories.Templating)]
public class DocumentParserReliabilityTests
{
    [TestMethod]
    public void TokenizeClassifiesInlineReplacementAndPreservesPositions()
    {
        var tokens = DocumentToken.Tokenize("first\n  {{ Name Red !}}", "test template");
        var open = tokens.Single(t => t.TokenType == DocumentTokenType.BeginReplacementSegment);
        var key = tokens.Single(t => t.TokenType == DocumentTokenType.ReplacementKey && string.IsNullOrWhiteSpace(t.Value) == false);
        var parameter = tokens.Single(t => t.TokenType == DocumentTokenType.ReplacementParameter && t.Value == "Red");
        var close = tokens.Single(t => t.TokenType == DocumentTokenType.QuickTerminateReplacementSegment);

        Assert.AreEqual(2, open.Line);
        Assert.AreEqual(3, open.Column);
        Assert.AreEqual("Name", key.Value);
        Assert.AreEqual("Red", parameter.Value);
        Assert.AreEqual("!}}", close.Value);
        Assert.AreEqual("test template", close.SourceFileLocation);
    }

    [TestMethod]
    public void RenderSupportsNestedBlocksAndInlineExpressions()
    {
        const string template = "{{if Enabled}}{{each item in Items}}[{{item.Name!}}] !{{each}}!{{if}}";
        var rendered = new DocumentRenderer().Render(template, new { Enabled = true, Items = new[] { new { Name = "one" }, new { Name = "two" } } });

        Assert.AreEqual("[one] [two] ", rendered.ToString());
    }

    [TestMethod]
    public void StandaloneControlLinesAreRemovedForLfAndCrLf()
    {
        var data = new { Enabled = true };
        var lf = new DocumentRenderer().Render("before\n{{if Enabled}}\ninside\n!{{if}}\nafter", data).ToString();
        var crlf = new DocumentRenderer().Render("before\r\n{{if Enabled}}\r\ninside\r\n!{{if}}\r\nafter", data).ToString();

        Assert.AreEqual("before\ninside\nafter", lf);
        Assert.AreEqual("before\r\ninside\r\nafter", crlf);
    }

    [TestMethod]
    public void LongPlainTextAndReplacementValuesAreSupported()
    {
        var longValue = new string('x', 20_000);
        var rendered = new DocumentRenderer().Render(longValue + "{{Value!}}", new { Value = longValue });

        Assert.AreEqual(longValue + longValue, rendered.ToString());
    }

    [TestMethod]
    public void TokenizationIsSafeAcrossConcurrentCallers()
    {
        Parallel.For(0, 500, i =>
        {
            var name = "value-" + i;
            var tokens = DocumentToken.Tokenize("prefix {{" + name + "!}} suffix", "template-" + i);
            Assert.AreEqual(name, tokens.Single(t => t.TokenType == DocumentTokenType.ReplacementKey).Value);
            Assert.IsTrue(tokens.All(t => t.SourceFileLocation == "template-" + i));
        });
    }

    [TestMethod]
    public void MismatchedClosingTagIsRejectedAtItsSourcePosition()
    {
        var exception = Assert.ThrowsExactly<DocumentRenderException>(() => new DocumentRenderer().Render("{{if Enabled}}text!{{each}}", new { Enabled = true }, "bad.template"));

        StringAssert.Contains(exception.Message, "Expected 'if', got 'each'");
        StringAssert.Contains(exception.Message, "bad.template");
    }

    [TestMethod]
    public void UnmatchedClosingTagIsRejected()
    {
        var exception = Assert.ThrowsExactly<DocumentRenderException>(() => new DocumentRenderer().Render("before !{{if}} after", new { Enabled = true }));

        StringAssert.Contains(exception.Message, "Unexpected token '!{{'");
    }

    [TestMethod]
    [DataRow("{{!}}", "replacement key")]
    [DataRow("{{Name", "'}}' or '!}}'")]
    [DataRow("{{if Enabled}}body", "end of 'if' replacement")]
    public void MalformedTemplatesReportUsefulErrors(string template, string expectedMessage)
    {
        var exception = Assert.ThrowsExactly<DocumentRenderException>(() => new DocumentRenderer().Render(template, new { Enabled = true }, "broken.template"));

        StringAssert.Contains(exception.Message, expectedMessage);
    }

    [TestMethod]
    public void NullTemplateIsRejectedAtThePublicBoundary()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new DocumentRenderer().Render((string)null!, new { }));
    }
}
