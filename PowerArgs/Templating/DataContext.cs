using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class DataContext
    {
        public LocalVariableSet LocalVariables{get;private set;}
        public object RootDataObject{get;private set;}

        public DataContext(object rootDataObject)
        {
            if(rootDataObject == null) throw new ArgumentNullException("rootDataObject cannot be null");
            this.LocalVariables = new LocalVariableSet();
            this.RootDataObject = rootDataObject;
        }

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
