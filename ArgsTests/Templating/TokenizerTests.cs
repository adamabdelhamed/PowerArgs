using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests.Templating
{
    [TestClass]
    public class TokenizerTests
    {
        [TestMethod]
        public void TestTokenizerBasicWhitespaceSeparatedStrings()
        {
            Tokenizer<Token> tokenizer = new Tokenizer<Token>();
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndExclude;
            var tokens = tokenizer.Tokenize("one two      three \n\n    \t   four\t\t\tfive\t\t\tsix");
            Assert.AreEqual(6, tokens.Count);
            Assert.AreEqual("one", tokens[0].Value);
            Assert.AreEqual("two", tokens[1].Value);
            Assert.AreEqual("three", tokens[2].Value);
            Assert.AreEqual("four", tokens[3].Value);
            Assert.AreEqual("five", tokens[4].Value);
            Assert.AreEqual("six", tokens[5].Value);

            Assert.AreEqual(1, tokens[0].Line);
            Assert.AreEqual(1, tokens[0].Column);

            Assert.AreEqual(1, tokens[1].Line);
            Assert.AreEqual(5, tokens[1].Column);

            Assert.AreEqual(3, tokens[3].Line);
            Assert.AreEqual(9, tokens[3].Column);
        }

        [TestMethod]
        public void TestTokenizerWithCustomDelimiterAndIncludedWhitespace()
        {
            var input = "{ Yo }";
            Tokenizer<Token> tokenizer = new Tokenizer<Token>();
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.Include;
            tokenizer.Delimiters.Add("{");
            tokenizer.Delimiters.Add("}");

            var tokens = tokenizer.Tokenize(input);
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual("{", tokens[0].Value);
            Assert.AreEqual(" Yo ", tokens[1].Value);
            Assert.AreEqual("}", tokens[2].Value);
        }



        [TestMethod]
        public void TestTokenizerWithCustomDelimiter()
        {
            var input = "A.B.C.D";
            Tokenizer<Token> tokenizer = new Tokenizer<Token>();
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndExclude;
            tokenizer.Delimiters.Add(".");
            var tokens = tokenizer.Tokenize(input);
            Assert.AreEqual(7, tokens.Count);
            Assert.AreEqual("A", tokens[0].Value);
            Assert.AreEqual(".", tokens[1].Value);
            Assert.AreEqual("B", tokens[2].Value);
            Assert.AreEqual(".", tokens[3].Value);
            Assert.AreEqual("C", tokens[4].Value);
            Assert.AreEqual(".", tokens[5].Value);
            Assert.AreEqual("D", tokens[6].Value);
        }

        [TestMethod]
        public void TestTokenizerWithCustomDelimiterAndWhitespace()
        {
            var input = "    \n\n\n\t\t\t   A  .    B       \n\n\n\t\t\t.\t\t\tC\t\t\t\t   .  D\t\t\t\t    \n\n\n";
            Tokenizer<Token> tokenizer = new Tokenizer<Token>();
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndExclude;
            tokenizer.Delimiters.Add(".");
            var tokens = tokenizer.Tokenize(input);
            Assert.AreEqual(7, tokens.Count);
            Assert.AreEqual("A", tokens[0].Value);
            Assert.AreEqual(".", tokens[1].Value);
            Assert.AreEqual("B", tokens[2].Value);
            Assert.AreEqual(".", tokens[3].Value);
            Assert.AreEqual("C", tokens[4].Value);
            Assert.AreEqual(".", tokens[5].Value);
            Assert.AreEqual("D", tokens[6].Value);
        }

        [TestMethod]
        public void TestTokenizerBasicWhitespaceSeparatedStringsWithWhitespaceIncluded()
        {
            var input = "one two      three \n\n    \t   four\t\t\tfive\t\t\tsix";
            Tokenizer<Token> tokenizer = new Tokenizer<Token>();
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndInclude;
            var tokens = tokenizer.Tokenize(input);

            var reconstructed = "";
            bool? lastTokenWasWhitespace = null;
            Token lastToken = null;
            foreach (var token in tokens)
            {
                if(token.Value == null)
                {
                    Assert.Fail("Unexpected null valued token");
                }
                else if(string.IsNullOrWhiteSpace(token.Value))
                {
                    lastTokenWasWhitespace = true;
                }
                else
                {
                    if(lastTokenWasWhitespace.HasValue && lastTokenWasWhitespace.Value == false)
                    {
                        Assert.Fail("2 consecutive non-whitespace tokens encountered.");
                    }
                    lastTokenWasWhitespace = false;
                }

                reconstructed += token.Value;
                lastToken = token;
            }

            Assert.AreEqual(input, reconstructed);
        }

        [TestMethod]
        public void TestTokenizerQuoteHandling()
        {
            var input = "Hi, my name is \"John Smith\"";
            Tokenizer<Token> tokenizer = new Tokenizer<Token>();
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndExclude;
            tokenizer.DoubleQuoteBehavior = DoubleQuoteBehavior.IncludeQuotedTokensAsStringLiterals;
            var tokens = tokenizer.Tokenize(input);
            AssertEqual(tokens, "Hi,", "my", "name", "is", "\"John Smith\"");

            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndInclude;
            tokenizer.DoubleQuoteBehavior = DoubleQuoteBehavior.NoSpecialHandling;
            tokenizer.Delimiters.Add("\"");
            tokens = tokenizer.Tokenize(input);
            AssertEqual(tokens, "Hi,", " ", "my", " ", "name", " ", "is", " ", "\"", "John", " ", "Smith", "\"");
        }

        [TestMethod]
        public void TestTokenizerQuoteHandlingWithEscapeSequenceWithinQuotes()
        {
            var input = "Hi, my name is \"John \\\"Smith\\\"\"";
            Tokenizer<Token> tokenizer = new Tokenizer<Token>();
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndExclude;
            tokenizer.DoubleQuoteBehavior = DoubleQuoteBehavior.IncludeQuotedTokensAsStringLiterals;
            var tokens = tokenizer.Tokenize(input);
            AssertEqual(tokens, "Hi,", "my", "name", "is", "\"John \"Smith\"\"");
        }


        [TestMethod]
        public void TestTokenizerQuoteHandlingWithEscapeSequenceBlockingQuotes()
        {
            var input = "Hi, my name is \\\"John Smith\\\"";
            Tokenizer<Token> tokenizer = new Tokenizer<Token>();
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndExclude;
            tokenizer.DoubleQuoteBehavior = DoubleQuoteBehavior.IncludeQuotedTokensAsStringLiterals;
            tokenizer.Delimiters.Add("\"");
            var tokens = tokenizer.Tokenize(input);
            AssertEqual(tokens, "Hi,", "my", "name", "is", "\"John", "Smith\"");
        }

        private void AssertEqual(List<Token> tokens, params string[] expected)
        {
            Assert.AreEqual(expected.Length, tokens.Count);
            for(int i = 0; i < tokens.Count; i++)
            {
                Assert.AreEqual(expected[i], tokens[i].Value);
            }
        }
    }
}
