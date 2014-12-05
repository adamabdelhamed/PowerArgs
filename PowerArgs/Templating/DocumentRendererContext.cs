using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// An object that tracks information about document rendering
    /// </summary>
    public class DocumentRendererContext
    {
        /// <summary>
        /// The current set of local variables that can be referenced by a template
        /// </summary>
        public LocalVariableSet LocalVariables{get;private set;}

        /// <summary>
        /// The root data object that is bound to the document template
        /// </summary>
        public object RootDataObject{get;private set;}

        /// <summary>
        /// The object that is currently rendering a document
        /// </summary>
        public DocumentRenderer DocumentRenderer { get; internal set; }

        /// <summary>
        /// Creates a render context from a root data object
        /// </summary>
        /// <param name="rootDataObject">The root data object that will be bound to a template</param>
        public DocumentRendererContext(object rootDataObject)
        {
            if(rootDataObject == null) throw new ArgumentNullException("rootDataObject cannot be null");
            this.LocalVariables = new LocalVariableSet();
            this.RootDataObject = rootDataObject;
        }

        /// <summary>
        /// Dynamically renders the given template.
        /// </summary>
        /// <param name="dynamicTemplate">The template to render</param>
        /// <param name="nestedToken">The token in the original document that resulted in dynamic rendering</param>
        /// <returns>The rendered content</returns>
        public ConsoleString RenderDynamicContent(string dynamicTemplate, DocumentToken nestedToken)
        {
            return this.DocumentRenderer.Render(dynamicTemplate, this, "dynamic evaluation sourced from '" + nestedToken.Position + "'");
        }

        /// <summary>
        /// Renders the given tokens, which are generally the body of a replacement expression.
        /// </summary>
        /// <param name="body">The tokens to render</param>
        /// <returns>the rendered content</returns>
        public ConsoleString RenderBody(IEnumerable<DocumentToken> body)
        {
            return DocumentRenderer.Render(body, this);
        }

        /// <summary>
        /// Evaluates the given expression and returns the result.  The expression can refer to a local variable (e.g. 'somevariable'), 
        /// a path starting from a localVariable (e.g. 'somevariable.SomeProperty.SomeOtherProperty'), or a path starting from the root
        /// data object (e.g. if the root was of type 'Person' you could say 'FirstName', assuming the Person type has a property called 'FirstName').
        /// </summary>
        /// <param name="expressionText">The expression text</param>
        /// <returns>The resolved value as a .NET object</returns>
        public object EvaluateExpression(string expressionText)
        {
            object localVariableValue;
            string restOfExpressionText;

            ObjectPathExpression expression;
            object root;

            if (LocalVariables.TryParseLocalVariable(expressionText, out localVariableValue, out restOfExpressionText))
            {
                if (restOfExpressionText == null)
                {
                    return localVariableValue;
                }
                else
                {
                    expression = ObjectPathExpression.Parse(restOfExpressionText);
                    root = localVariableValue;
                }
            }
            else
            {
                expression = ObjectPathExpression.Parse(expressionText);
                root = this.RootDataObject;
            }

            var ret = expression.Evaluate(root);
            return ret;
        }
    }
}
