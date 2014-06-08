using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class PlainTextDocumentExpression : IDocumentExpression
    {
        private List<DocumentToken> tokens;

        public PlainTextDocumentExpression(List<DocumentToken> tokens)
        {
            this.tokens = tokens;
        }
        public PlainTextDocumentExpression(DocumentToken singleToken) : this(new List<DocumentToken> { singleToken }) { }
        public string Evaluate(DataContext context)
        {
            var ret = "";

            foreach (var token in this.tokens)
            {
                ret += token.Value;
            }

            return ret;
        }
    }
}
