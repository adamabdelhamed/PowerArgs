using System;
using System.Collections.Generic;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// An exception that is thrown while trying to render a templated document
    /// </summary>
    public class DocumentRenderException : Exception
    {
        /// <summary>
        /// A reason why a token was not provided when creating this exception
        /// </summary>
        public enum NoTokenReason
        {
            /// <summary>
            /// Indicates that there is no token specified because there was an unexpected end of string.
            /// </summary>
            EndOfString,
        }

        /// <summary>
        /// Creates an exception given a message and the offending token
        /// </summary>
        /// <param name="msg">The exception message</param>
        /// <param name="offendingToken">The offending token</param>
        public DocumentRenderException(string msg, DocumentToken offendingToken) : base(msg + ": " + offendingToken.Position) { }

        /// <summary>
        /// Creates an exception given a message and a reason why no token was provided
        /// </summary>
        /// <param name="msg">The exception message</param>
        /// <param name="reason">The reason why no token was provided</param>
        public DocumentRenderException(string msg, NoTokenReason reason) : base(msg + ": " + LookupReason(reason)) { }

        private static string LookupReason(NoTokenReason reason)
        {
            if(reason == NoTokenReason.EndOfString)
            {
                return "End of string";
            }
            else
            {
                throw new ArgumentException("Unknown reason: " + reason);
            }
        }
    }

    /// <summary>
    /// A class that describes a document template
    /// </summary>
    public class DocumentTemplateInfo
    {
        /// <summary>
        /// The template text value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The template's source location.  This is usually a file location, but it does not have to be.
        /// </summary>
        public string SourceLocation { get; set; }
    }

    /// <summary>
    /// A class used to render template documents.
    /// </summary>
    public class DocumentRenderer
    {
        private DocumentExpressionParser expressionParser;
        private Dictionary<string, DocumentTemplateInfo> namedTemplates;

        /// <summary>
        /// Creates a new DocumentRenderer.
        /// </summary>
        public DocumentRenderer()
        {
            namedTemplates = new Dictionary<string, DocumentTemplateInfo>();
            expressionParser = new DocumentExpressionParser();
        }

        /// <summary>
        /// Gets the underlying expression parser
        /// </summary>
        public DocumentExpressionParser ExpressionParser
        {
            get
            {
                return expressionParser;
            }
        }

        /// <summary>
        /// Renders a document from a template, using a plain old .NET object as a data source.
        /// </summary>
        /// <param name="template">The template to use</param>
        /// <param name="data">The data source to use for template replacements</param>
        /// <returns></returns>
        public ConsoleString Render(DocumentTemplateInfo template, object data)
        {
            return Render(template.Value, data, template.SourceLocation);
        }

        /// <summary>
        /// Renders a document from a template, using a plain old .NET object as a data source.
        /// </summary>
        /// <param name="template">The template to use</param>
        /// <param name="data">The data source to use for template replacements</param>
        /// <param name="sourceFileLocation">The source of the template, used when reporting back errors</param>
        /// <returns>The rendered document</returns>
        public ConsoleString Render(string template, object data, string sourceFileLocation = null)
        {
            return Render(template, new DocumentRendererContext(data) { DocumentRenderer = this }, sourceFileLocation);
        }

        /// <summary>
        /// Register a named tamplate that can be accessed by other templates
        /// </summary>
        /// <param name="name">The unique name of the template</param>
        /// <param name="info">The template info</param>
        public void RegisterTemplate(string name, DocumentTemplateInfo info)
        {
            if(namedTemplates.ContainsKey(name))
            {
                throw new ArgumentException("There is already a template named '" + name + "'");
            }

            namedTemplates.Add(name, info);
        }
        
        /// <summary>
        /// Unregister a named template.
        /// </summary>
        /// <param name="name">The name of the template to unregister</param>
        public void UnregisterTemplate(string name)
        {
            bool removed = namedTemplates.Remove(name);
            if(removed == false)
            {
                throw new KeyNotFoundException("There is no templated named '" + name + "'");
            }
        }

        internal ConsoleString Render(string template, DocumentRendererContext context, string sourceFileLocation = null)
        {
            List<DocumentToken> tokens = DocumentToken.Tokenize(template, sourceFileLocation);
            return Render(tokens, context);
        }

        internal DocumentTemplateInfo GetTemplate(DocumentToken nameToken)
        {
            DocumentTemplateInfo ret;
            if (namedTemplates.TryGetValue(nameToken.Value, out ret) == false)
            {
                throw new DocumentRenderException("There is no templated named '" + nameToken.Value + "'", nameToken);
            }
            return ret;
        }

        internal ConsoleString Render(IEnumerable<DocumentToken> tokens, DocumentRendererContext context)
        {
            var expressions = expressionParser.Parse(tokens);
            var ret = Evaluate(expressions, context);
            return ret;
        }

        private ConsoleString Evaluate(List<IDocumentExpression> expressions, DocumentRendererContext context)
        {
            ConsoleString ret = new ConsoleString();

            foreach (var expression in expressions)
            {
                var eval = expression.Evaluate(context);
                ret += eval;
            }

            return ret;
        }
    }
}
