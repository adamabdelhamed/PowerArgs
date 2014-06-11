using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class ConsoleColorStackElement
    {
        public ConsoleColor? FG { get; set; }
        public ConsoleColor? BG { get; set; }
    }

    public class LocalVariableSet
    {
        private Dictionary<string, object> localVariables;

        private Stack<ConsoleColorStackElement> consoleStack;

        public LocalVariableSet()
        {
            this.localVariables = new Dictionary<string, object>();
            consoleStack = new Stack<ConsoleColorStackElement>();
        }

        public void PushConsoleColors(ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            ConsoleColorStackElement el = new ConsoleColorStackElement();
            if(IsDefined("ConsoleForegroundColor"))
            {
                el.FG = (ConsoleColor)this["ConsoleForegroundColor"];
            }

            if (IsDefined("ConsoleBackgroundColor"))
            {
                el.BG = (ConsoleColor)this["ConsoleBackgroundColor"];
            }
            consoleStack.Push(el);

            if(fg.HasValue)
            {
                Force("ConsoleForegroundColor", fg.Value);
            }

            if (bg.HasValue)
            {
                Force("ConsoleBackgroundColor", bg.Value);
            }
        }

        public void PopConsoleColors()
        {
            var popped = consoleStack.Pop();

            if(popped.FG.HasValue)
            {
                Force("ConsoleForegroundColor", popped.FG.Value);
            }
            else
            {
                ForceClear("ConsoleForegroundColor");
            }

            if (popped.BG.HasValue)
            {
                Force("ConsoleBackgroundColor", popped.BG.Value);
            }
            else
            {
                ForceClear("ConsoleBackgroundColor");
            }
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

        public void Force(string variableName, object value)
        {
            if (localVariables.ContainsKey(variableName))
            {
                localVariables[variableName] = value;
            }
            else
            {
                localVariables.Add(variableName, value);
            }
        }

        public void Remove(DocumentToken variableToken)
        {
            if (localVariables.Remove(variableToken.Value) == false)
            {
                throw new ArgumentException("There is no variable to remove called '" + variableToken.Value + "' at " + variableToken.Position);
            }
        }

        public void ForceClear(string variableName)
        {
            localVariables.Remove(variableName);
        }


        public bool IsDefined(string variableName)
        {
            return localVariables.ContainsKey(variableName);
        }

        public object this[string variableName]
        {
            get
            {
                return localVariables[variableName];
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
