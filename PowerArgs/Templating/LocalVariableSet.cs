using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class LocalVariableSet
    {
        private Dictionary<string, object> localVariables;

        public LocalVariableSet()
        {
            this.localVariables = new Dictionary<string, object>();
        }

        public void Add(DocumentToken variableToken, object value)
        {
            if(localVariables.ContainsKey(variableToken.Value))
            {
                throw new ArgumentException("There is already a variable called '" + variableToken.Value + "' at " + variableToken.Position);
            }
            else
            {
                localVariables.Add(variableToken.Value, value);
            }
        }

        public void Remove(DocumentToken variableToken)
        {
            if (localVariables.Remove(variableToken.Value) == false)
            {
                throw new ArgumentException("There is no variable to remove called '" + variableToken.Value + "' at " + variableToken.Position);
            }
        }

        public bool TryParseLocalVariable(string expression, out object result, out string restOfExpression)
        {
            var rootVariableIdentifier = "";
            restOfExpression = null;
            for (var i = 0; i < expression.Length; i++)
            {
                var character = expression[i];
                // these are the only 2 characters that are allowed to come after an identifier.
                if (character == '.' || character == '[')
                {
                    if (i == expression.Length - 1)
                    {
                        restOfExpression = "";
                    }
                    else
                    {
                        restOfExpression = expression.Substring(i + 1);
                    }
                    break;
                }
                else
                {
                    rootVariableIdentifier += character;
                }
            }

            return localVariables.TryGetValue(rootVariableIdentifier, out result);
        }
    }
}
