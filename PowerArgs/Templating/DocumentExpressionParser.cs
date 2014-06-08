using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public interface IDocumentExpressionProvider
    {
        IDocumentExpression CreateExpression(List<DocumentToken> parameters, List<DocumentToken> body);
    }

    public class DocumentExpressionParser
    {
        public Dictionary<string, IDocumentExpressionProvider> ExpressionProviders { get; private set; }

        public DocumentExpressionParser()
        {
            this.ExpressionProviders = new Dictionary<string, IDocumentExpressionProvider>();
            this.ExpressionProviders.Add("if", new IfExpressionProvider());
            this.ExpressionProviders.Add("each", new EachExpressionProvider());
        }

        public List<IDocumentExpression> Parse(List<DocumentToken> tokens, string scopeKey = null, int numSameScopesOpen = 0)
        {
            List<IDocumentExpression> ret = new List<IDocumentExpression>();

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(tokens);

            DocumentToken currentToken;

            while(reader.CanAdvance())
            {
                if(reader.Peek().TokenType == DocumentTokenType.BeginReplacementSegment)
                {
                    ParseReplacement(reader, ret);
                }
                else
                {
                    var plain = new PlainTextDocumentExpression(reader.Advance());
                    ret.Add(plain);
                }
            }

            return ret;
        }

        private void ParseReplacement(TokenReader<DocumentToken> reader, List<IDocumentExpression> ret)
        {
            var openToken = AdvanceAndExpect(reader, DocumentTokenType.BeginReplacementSegment, "{{");
            var replacementKeyToken = AdvanceAndExpect(reader, DocumentTokenType.ReplacementKey, "replacement key", skipWhitespace: true);

            List<DocumentToken> parameters = new List<DocumentToken>();
            List<DocumentToken> body = new List<DocumentToken>();
            while (reader.CanAdvance(skipWhitespace: true) && reader.Peek(skipWhitespace: true).TokenType == DocumentTokenType.ReplacementParameter)
            {
                var paramToken = reader.Advance(skipWhitespace: true);
                parameters.Add(paramToken);
            }

            if(reader.CanAdvance(skipWhitespace: true) == false)
            {
                throw new ArgumentException("Expected '}}' or '!}}', got end of string");
            }

            reader.Advance(skipWhitespace: true);
            if (reader.CurrentToken.TokenType == DocumentTokenType.EndReplacementSegment)
            {
                body.AddRange(ReadReplacementBody(reader, replacementKeyToken));
            }
            else if (reader.CurrentToken.TokenType == DocumentTokenType.QuickTerminateReplacementSegment)
            {
                // do nothing, there is no body
            }
            else
            {
                throw Unexpected("}} or !}}", reader.CurrentToken);
            }

            IDocumentExpressionProvider provider;
            if(this.ExpressionProviders.TryGetValue(replacementKeyToken.Value, out provider) == false)
            {
                parameters.Add(replacementKeyToken);
                provider = new EvalExpressionProvider();
            }

            var expression = provider.CreateExpression(parameters, body);
            ret.Add(expression);
        }

        private List<DocumentToken> ReadReplacementBody(TokenReader<DocumentToken> reader, DocumentToken replacementKeyToken)
        {
            List<DocumentToken> replacementContents = new List<DocumentToken>();

            int numOpenReplacements = 1;

            while (reader.CanAdvance())
            {
                if (reader.Peek().TokenType == DocumentTokenType.BeginReplacementSegment)
                {
                    numOpenReplacements++;
                }
                else if (reader.Peek().TokenType == DocumentTokenType.QuickTerminateReplacementSegment)
                {
                    numOpenReplacements--;

                    if(numOpenReplacements == 0)
                    {
                        throw Unexpected(reader.Peek());
                    }

                }
                else if (reader.Peek().TokenType == DocumentTokenType.BeginTerminateReplacementSegment)
                {
                    numOpenReplacements--;

                    if(numOpenReplacements == 0)
                    {
                        AdvanceAndExpect(reader, DocumentTokenType.BeginTerminateReplacementSegment, "!{{", skipWhitespace: true);
                        AdvanceAndExpect(reader, DocumentTokenType.ReplacementKey, replacementKeyToken.Value, skipWhitespace: true);
                        AdvanceAndExpect(reader, DocumentTokenType.EndReplacementSegment, "}}", skipWhitespace: true);
                        break;
                    }
                }

                replacementContents.Add(reader.Advance());
            }
 
            if(numOpenReplacements != 0)
            {
                throw Unexpected("end of " + replacementKeyToken.Value + " replacement");
            }

            return replacementContents;

        }

        private DocumentToken AdvanceAndExpect(TokenReader<DocumentToken> reader, DocumentTokenType expectedType, string expectedText, bool skipWhitespace = false)
        {
            if(reader.CanAdvance(skipWhitespace) == false)
            {
                throw Unexpected(expectedText);
            }

            var read = reader.Advance(skipWhitespace: skipWhitespace);
            if (read.TokenType != expectedType)
            {
                throw Unexpected(expectedText, reader.CurrentToken);
            }
            return read;
        }

        private Exception Unexpected(string expected, Token actual = null)
        {
            if (actual != null)
            {
                var format = "Expected '{0}', got '{1}' at {2}";
                var msg = string.Format(format, expected, actual.Value, actual.Position);
                return new ArgumentException(msg);
            }
            else
            {
                var format = "Expected '{0}', got end of string";
                var msg = string.Format(format, expected);
                return new ArgumentException(msg);
            }
        }

        private Exception Unexpected(Token t)
        {
            var format = "Unexpected '{0}' at {1}";
            var msg = string.Format(format, t.Value, t.Position);
            return new ArgumentException(msg);
        }
    }
}
