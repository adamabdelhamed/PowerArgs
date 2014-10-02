using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// An element that can be used to track the state of the renderer's colors as expressions are being evaluated
    /// </summary>
    public class ConsoleColorStackElement
    {
        /// <summary>
        /// An optional foreground color value
        /// </summary>
        public ConsoleColor? FG { get; set; }

        /// <summary>
        /// An optional background color value
        /// </summary>
        public ConsoleColor? BG { get; set; }
    }

    /// <summary>
    /// A class that lets the document renderer track local variable state as expressions are being evaluated
    /// </summary>
    public class LocalVariableSet
    {
        private Dictionary<string, object> localVariables;

        private Stack<ConsoleColorStackElement> consoleStack;


        /// <summary>
        /// Creates a new local variable set
        /// </summary>
        public LocalVariableSet()
        {
            this.localVariables = new Dictionary<string, object>();
            consoleStack = new Stack<ConsoleColorStackElement>();
        }

        /// <summary>
        /// Gets the current foreground color that is in scope or null if it is not currently defined
        /// </summary>
        public ConsoleColor? CurrentForegroundColor
        {
            get
            {
                if (IsDefined("ConsoleForegroundColor"))
                {
                    return (ConsoleColor)this["ConsoleForegroundColor"];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the current background color that is in scope or null if it is not currently defined
        /// </summary>
        public ConsoleColor? CurrentBackgroundColor
        {
            get
            {
                if (IsDefined("ConsoleBackgroundColor"))
                {
                    return (ConsoleColor)this["ConsoleBackgroundColor"];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Sets the current console color variables to the given values, pushing the existing values onto a stack
        /// </summary>
        /// <param name="fg">The optional foreground color to set</param>
        /// <param name="bg">The optional background color to set</param>
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

        /// <summary>
        /// Pops the latest console colors off of the stack and sets them as the current colors to use.
        /// </summary>
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

        /// <summary>
        /// Adds a new local variable.  This will throw if there's already a variable defined with the given identifier
        /// </summary>
        /// <param name="variableToken">The token containing the identifier of the variable name</param>
        /// <param name="value">The initial value of the local variable</param>
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

        /// <summary>
        /// Forefully sets the value of a local value, regardless of whether or not that variable is already defined.
        /// </summary>
        /// <param name="variableName">The name of the variable</param>
        /// <param name="value">The value of the variable</param>
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

        /// <summary>
        /// Removes a local variable.  This will throw if there is no variable with the given name
        /// </summary>
        /// <param name="variableToken">The token containing the identifier of the variable to remove</param>
        public void Remove(DocumentToken variableToken)
        {
            if (localVariables.Remove(variableToken.Value) == false)
            {
                throw new ArgumentException("There is no variable to remove called '" + variableToken.Value + "' at " + variableToken.Position);
            }
        }

        /// <summary>
        /// Tries to remove a variable, and does not throw if the variable is not defined.
        /// </summary>
        /// <param name="variableName">The name of the variable to clear</param>
        public void ForceClear(string variableName)
        {
            localVariables.Remove(variableName);
        }

        /// <summary>
        /// Determines if the given variable is defined
        /// </summary>
        /// <param name="variableName">The name to check</param>
        /// <returns>True if the variable is defined, false otherwise</returns>
        public bool IsDefined(string variableName)
        {
            return localVariables.ContainsKey(variableName);
        }

        /// <summary>
        /// Gets the value of a local variable given an identifier
        /// </summary>
        /// <param name="variableName">the name of the variable to lookup</param>
        /// <returns></returns>
        public object this[string variableName]
        {
            get
            {
                if(IsDefined(variableName) == false)
                {
                    throw new ArgumentException("There is no local variable called '" + variableName + "'");
                }

                return localVariables[variableName];
            }
        }

        /// <summary>
        /// Tries to parse a local variable from an expression.  If successful the expression is evaluated then the local variable is returned via an out variable.  
        /// If there was more to the expression, a property navigation for example, then that is also passed to the caller via an out variable.
        /// </summary>
        /// <param name="expression">The expression that may refer to a local variable</param>
        /// <param name="result">The local variable that is set if a local variable was matched</param>
        /// <param name="restOfExpression">The rest of the expression that came after the local variable identifier</param>
        /// <returns>True if a local variable was parsed, false otherwise</returns>
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
