using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// An object that can take a replacement key, a set of parameters, an optional body, and transform it into a document expression.
    /// </summary>
    public interface IDocumentExpressionProvider
    {
        /// <summary>
        /// Transforms document expression context into a document expression
        /// </summary>
        /// <param name="context">Context about the expression being parsed</param>
        /// <returns>The formal expression that can be evaluated into text</returns>
        IDocumentExpression CreateExpression(DocumentExpressionContext context);
    }

    /// <summary>
    /// An object that contains contextual information that is useful for expression providers
    /// </summary>
    public class DocumentExpressionContext
    {
        /// <summary>
        /// The opening token of the replacement expression '{{'
        /// </summary>
        public DocumentToken OpenToken { get; internal set; }

        /// <summary>
        /// The closing topen of the replacement expression '!}}' or '!{{TAG}}'
        /// </summary>
        public DocumentToken CloseToken { get; internal set; }

        /// <summary>
        /// The replacement key token.  Example: 'if' in {{ if Foo }}
        /// </summary>
        public DocumentToken ReplacementKeyToken { get; internal set; }

        /// <summary>
        /// The parameters of the replacement.  Example: ["Foo", "Bar"] in '{{ someTag Foo Bar !}}'
        /// </summary>
        public ReadOnlyCollection<DocumentToken> Parameters { get; internal set; }

        /// <summary>
        /// The body text between the tags.  Not populated if the tag is quick terminated.
        /// </summary>
        public ReadOnlyCollection<DocumentToken> Body { get; internal set; }
    }

    /// <summary>
    /// An attribute that can be added to a class that implements IDocumentExpressionProvider.  This attributes indicates that the provider can be
    /// dynamically registered.
    /// </summary>
    public class DynamicExpressionProviderAttribute : Attribute
    {
        /// <summary>
        /// The replacement key (e.g. 'each' in {{each foo in bar }}) to use when the given provider is registered
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Creates a new DynamicExpressionProviderAttribute given a key
        /// </summary>
        /// <param name="key"></param>
        public DynamicExpressionProviderAttribute(string key)
        {
            this.Key = key;
        }
    }

    /// <summary>
    /// A class that can parse a collection of document tokens into a collection of expressions
    /// </summary>
    public class DocumentExpressionParser
    {
        private Dictionary<string, IDocumentExpressionProvider> expressionProviders;

        /// <summary>
        /// Gets a list of registered expression provider keys
        /// </summary>
        public List<String> RegisteredReplacementExpressionProviderKeys
        {
            get
            {
                return this.expressionProviders.Keys.ToList();
            }
        }

        /// <summary>
        /// Creates a new document expression parser and registers the built in expression provider types.
        /// </summary>
        public DocumentExpressionParser()
        {
            this.expressionProviders = new Dictionary<string, IDocumentExpressionProvider>();
            this.expressionProviders.Add("if", new IfExpressionProvider(false));
            this.expressionProviders.Add("ifnot", new IfExpressionProvider(true));
            this.expressionProviders.Add("each", new EachExpressionProvider());
            this.expressionProviders.Add("var", new VarExpressionProvider());
            this.expressionProviders.Add("clearvar", new ClearVarExpressionProvider());
            this.expressionProviders.Add("table", new TableExpressionProvider());
            this.expressionProviders.Add("template", new TemplateExpressionProvider());
        }

        /// <summary>
        /// Searches the given assembly for IDocumentExpressionProvider types that have the DynamicExpressionProviderAttribute attribute and registers those providers
        /// with this parser.  The type needs to have a default constructor.
        /// </summary>
        /// <param name="target">The assembly to search</param>
        /// <param name="allowOverrideExistingKeys">If true, providers with keys that are already exist with override the existing providers.  If false, an exception will be thrown if a conflict is found.</param>
        public void RegisterDynamicReplacementExpressionProviders(Assembly target, bool allowOverrideExistingKeys = false)
        {
            var providerTypes = from type in target.GetTypes() where type.GetInterfaces().Contains(typeof(IDocumentExpressionProvider)) && type.HasAttr<DynamicExpressionProviderAttribute>() select type;
            foreach (var providerType in providerTypes)
            {
                var provider = (IDocumentExpressionProvider)Activator.CreateInstance(providerType);
                var key = providerType.Attr<DynamicExpressionProviderAttribute>().Key;
                RegisterReplacementExpressionProvider(key, provider, allowOverrideExistingKeys);
            }
        }

        /// <summary>
        /// Manually registers the given expression provider using the given key.  
        /// </summary>
        /// <param name="replacementKey">The unique key for the replacement provider (e.g. 'each' in {{each foo in bar}}</param>
        /// <param name="provider">The provider to register</param>
        /// <param name="allowOverrideExistingKeys">If true, allow this provider to replace an existing provider registered with the same key .  If false, an exception will be thrown if a conflict is found.</param>
        public void RegisterReplacementExpressionProvider(string replacementKey, IDocumentExpressionProvider provider, bool allowOverrideExistingKeys = false)
        {
            if(this.expressionProviders.ContainsKey(replacementKey))
            {
                if (allowOverrideExistingKeys)
                {
                    this.expressionProviders[replacementKey] = provider;
                }
                else
                {
                    throw new ArgumentException("A replacement expression provider withe key '" + replacementKey + "' already exists.  Use the allowOverrideExistingKeys flag to allow overriding the key");
                }
            }
            else
            {
                this.expressionProviders.Add(replacementKey, provider);
            }
        }

        /// <summary>
        /// Unregisters the expression provider with the given key
        /// </summary>
        /// <param name="replacementKey">The key of the provider to unregister</param>
        public void UnregisterReplacementExpressionProvider(string replacementKey)
        {
            bool removed = this.expressionProviders.Remove(replacementKey);
            if(removed == false)
            {
                throw new ArgumentException("There is no replacement expression provider with key '" + replacementKey + "'");
            }
        }

        /// <summary>
        /// Parses the given tokens into document expressions that can then be evaluated against a data context.
        /// </summary>
        /// <param name="tokens">The tokens to parse</param>
        /// <returns>a list of document expressions</returns>
        public List<IDocumentExpression> Parse(IEnumerable<DocumentToken> tokens)
        {
            List<IDocumentExpression> ret = new List<IDocumentExpression>();

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(tokens);

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


            DocumentToken closeReplacementToken;
            if(reader.TryAdvance(out closeReplacementToken, skipWhitespace: true) == false)
            {
                throw Unexpected(string.Format("'{0}' or '{1}'", DocumentToken.GetTokenTypeValue(DocumentTokenType.EndReplacementSegment), DocumentToken.GetTokenTypeValue(DocumentTokenType.QuickTerminateReplacementSegment)));
            }

            if (closeReplacementToken.TokenType == DocumentTokenType.EndReplacementSegment)
            {
                body.AddRange(ReadReplacementBody(reader, replacementKeyToken));
            }
            else if (closeReplacementToken.TokenType == DocumentTokenType.QuickTerminateReplacementSegment)
            {
                // do nothing, there is no body when the quick termination replacement segment is used
            }
            else
            {
                throw Unexpected(string.Format("'{0}' or '{1}'", DocumentToken.GetTokenTypeValue(DocumentTokenType.EndReplacementSegment), DocumentToken.GetTokenTypeValue(DocumentTokenType.QuickTerminateReplacementSegment)), closeReplacementToken);
            }

            IDocumentExpressionProvider provider;
            if (this.expressionProviders.TryGetValue(replacementKeyToken.Value, out provider) == false)
            {
                provider = new EvalExpressionProvider();
            }

            var context = new DocumentExpressionContext
            {
                OpenToken = openToken,
                CloseToken = closeReplacementToken,
                Parameters = parameters.AsReadOnly(),
                Body = body.AsReadOnly(),
                ReplacementKeyToken = replacementKeyToken,
            };

            var expression = provider.CreateExpression(context);
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
                throw Unexpected(DocumentToken.GetTokenTypeValue(expectedType), read);
            }
            return read;
        }

        private DocumentToken AdvanceAndExpect(TokenReader<DocumentToken> reader, DocumentTokenType expectedType, string expectedText, bool skipWhitespace = false)
        {

            DocumentToken read;
            if(reader.TryAdvance(out read,skipWhitespace: skipWhitespace) == false)
            {
                throw Unexpected(expectedText);
            }

            if (read.TokenType != expectedType)
            {
                throw Unexpected(expectedText, read);
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
