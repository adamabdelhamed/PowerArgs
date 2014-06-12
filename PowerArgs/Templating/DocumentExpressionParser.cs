using System.Collections.Generic;

namespace PowerArgs
{
    public interface IDocumentExpressionProvider
    {
        IDocumentExpression CreateExpression(DocumentToken replacementKeyToken, List<DocumentToken> parameters, List<DocumentToken> body);
    }

    public class DocumentExpressionParser
    {
        public Dictionary<string, IDocumentExpressionProvider> ExpressionProviders { get; private set; }

        public DocumentExpressionParser()
        {
            this.ExpressionProviders = new Dictionary<string, IDocumentExpressionProvider>();
            this.ExpressionProviders.Add("if", new IfExpressionProvider(false));
            this.ExpressionProviders.Add("ifnot", new IfExpressionProvider(true));
            this.ExpressionProviders.Add("each", new EachExpressionProvider());
            this.ExpressionProviders.Add("var", new VarExpressionProvider());
            this.ExpressionProviders.Add("clearvar", new ClearVarExpressionProvider());
            this.ExpressionProviders.Add("table", new TableExpressionProvider());
        }

        public List<IDocumentExpression> Parse(List<DocumentToken> tokens)
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
            var openToken = AdvanceAndExpectConstantType(reader, DocumentTokenType.BeginReplacementSegment);
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
                throw Unexpected(string.Format("'{0}' or '{1}'", DocumentToken.GetTokenTypeValue(DocumentTokenType.EndReplacementSegment), DocumentToken.GetTokenTypeValue(DocumentTokenType.QuickTerminateReplacementSegment)));
            }

            reader.Advance(skipWhitespace: true);
            if (reader.CurrentToken.TokenType == DocumentTokenType.EndReplacementSegment)
            {
                body.AddRange(ReadReplacementBody(reader, replacementKeyToken));
            }
            else if (reader.CurrentToken.TokenType == DocumentTokenType.QuickTerminateReplacementSegment)
            {
                // do nothing, there is no body when the quick termination replacement segment is used
            }
            else
            {
                throw Unexpected(string.Format("'{0}' or '{1}'", DocumentToken.GetTokenTypeValue(DocumentTokenType.EndReplacementSegment), DocumentToken.GetTokenTypeValue(DocumentTokenType.QuickTerminateReplacementSegment)), reader.CurrentToken);
            }

            IDocumentExpressionProvider provider;
            if(this.ExpressionProviders.TryGetValue(replacementKeyToken.Value, out provider) == false)
            {
                parameters.Insert(0, replacementKeyToken);
                provider = new EvalExpressionProvider();
            }

            var expression = provider.CreateExpression(replacementKeyToken, parameters, body);
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
                        AdvanceAndExpectConstantType(reader, DocumentTokenType.BeginTerminateReplacementSegment);
                        AdvanceAndExpect(reader, DocumentTokenType.ReplacementKey, replacementKeyToken.Value, skipWhitespace: true);
                        AdvanceAndExpectConstantType(reader, DocumentTokenType.EndReplacementSegment);
                        break;
                    }
                }

                replacementContents.Add(reader.Advance());
            }
 
            if(numOpenReplacements != 0)
            {
                throw Unexpected("end of '" + replacementKeyToken.Value + "' replacement");
            }

            return replacementContents;

        }

        private DocumentToken AdvanceAndExpectConstantType(TokenReader<DocumentToken> reader, DocumentTokenType expectedType)
        {
            DocumentToken read;
            if(reader.TryAdvance(out read,skipWhitespace: true) == false)
            {
                throw Unexpected(DocumentToken.GetTokenTypeValue(expectedType));
            }

            if (read.TokenType != expectedType)
            {
                throw Unexpected(DocumentToken.GetTokenTypeValue(expectedType), reader.CurrentToken);
            }
            return read;
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

        private DocumentRenderException Unexpected(string expected, DocumentToken actual = null)
        {
            if (actual != null)
            {
                return new DocumentRenderException(string.Format("Expected '{0}', got '{1}'", expected, actual.Value), actual);
            }
            else
            {
                var format = "Expected '{0}'";
                var msg = string.Format(format, expected);
                return new DocumentRenderException(msg, DocumentRenderException.NoTokenReason.EndOfString);
            }
        }

        private DocumentRenderException Unexpected(DocumentToken t)
        {
            return new DocumentRenderException(string.Format("Unexpected token '{0}'", t.Value), t);
        }
    }
}
