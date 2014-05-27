using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// A simple boolean expression parser that supports and '&amp;', or '|', and grouping via parentheses.
    /// </summary>
    public static class BooleanExpressionParser
    {
        /// <summary>
        /// Parses the given boolean expression which can be made up of variables and boolean operators (and '&amp;' and or '|') grouped by parentheses.
        /// </summary>
        /// <param name="expressionText">The expression to parse</param>
        /// <returns>The parsed expression</returns>
        public static BooleanExpression Parse(string expressionText)
        {
            List<BooleanExpressionToken> tokens = Tokenize(expressionText);
            BooleanExpression tree = BuildTree(tokens);
            return tree;
        }

        private static BooleanExpression BuildTree(List<BooleanExpressionToken> tokens)
        {
            BooleanExpression rootNode = new BooleanExpression();

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (token.Type == BooleanExpressionTokenType.GroupClose ||
                    (rootNode.Operands.Count == 0 && (token.Type == BooleanExpressionTokenType.And || token.Type == BooleanExpressionTokenType.Or)) ||
                    (i == tokens.Count - 1 && (token.Type == BooleanExpressionTokenType.And || token.Type == BooleanExpressionTokenType.Or)))
                {
                    throw new ArgumentException("Unexpected token '" + token.TokenValue + "'");
                }

                if (token.Type == BooleanExpressionTokenType.GroupOpen)
                {
                    int numOpen = 1;
                    List<BooleanExpressionToken> groupedExpression = new List<BooleanExpressionToken>();
                    for (int j = i + 1; j < tokens.Count; j++)
                    {
                        var innerToken = tokens[j];
                        if (innerToken.Type == BooleanExpressionTokenType.GroupClose && numOpen == 1)
                        {
                            numOpen = 0;
                            i = j;
                            break;
                        }
                        else if (innerToken.Type == BooleanExpressionTokenType.GroupClose)
                        {
                            numOpen--;
                        }
                        else if (innerToken.Type == BooleanExpressionTokenType.GroupOpen)
                        {
                            numOpen++;
                        }

                        groupedExpression.Add(innerToken);
                    }

                    if (numOpen > 0)
                    {
                        throw new ArgumentException("You are missing at least 1 closing parenthesis");
                    }

                    if (numOpen < 0)
                    {
                        throw new ArgumentException("You have at least 1 extra closing parenthesis");
                    }

                    if (groupedExpression.Count == 0)
                    {
                        throw new ArgumentException("You have an empty set of parenthesis");
                    }

                    rootNode.Operands.Add(BuildTree(groupedExpression));
                }
                else if (token.Type == BooleanExpressionTokenType.Variable)
                {
                    rootNode.Operands.Add(new BooleanExpressionVariable() { VariableName = token.TokenValue });
                }
                else if (token.Type == BooleanExpressionTokenType.And || token.Type == BooleanExpressionTokenType.Or)
                {
                    if (rootNode.Operators.Count > rootNode.Operands.Count)
                    {
                        throw new ArgumentException("You cannot have two consecutive operators '&&' or '||', use '&' or '|'");
                    }
                    rootNode.Operators.Add((BooleanOperator)token.Type);
                }
                else
                {
                    throw new ArgumentException("Unexpected token '" + token.TokenValue + "'");
                }
            }

            return rootNode;
        }

        private static List<BooleanExpressionToken> Tokenize(string expressionText)
        {
            List<BooleanExpressionToken> ret = new List<BooleanExpressionToken>();
            BooleanExpressionToken currentToken = null;
            List<char> specialsChars = new List<char>() { '(', ')', '&', '|' };

            foreach (var c in expressionText)
            {
                if(char.IsWhiteSpace(c))
                {
                    if(currentToken != null)
                    {
                        ret.Add(currentToken);
                        currentToken = null;
                    }
                    continue;
                }

                if (specialsChars.Contains(c))
                {
                    if (currentToken != null)
                    {
                        ret.Add(currentToken);
                        currentToken = null;
                    }

                    ret.Add(new BooleanExpressionToken() { TokenValue = c + "", Type = (BooleanExpressionTokenType)Enum.ToObject(typeof(BooleanExpressionTokenType), ((int)c)) });
                }
                else
                {
                    if (currentToken == null)
                    {
                        currentToken = new BooleanExpressionToken() { Type = BooleanExpressionTokenType.Variable };
                        currentToken.TokenValue = "" + c;
                    }
                    else
                    {
                        currentToken.TokenValue += c;
                    }
                }
            }

            if (currentToken != null)
            {
                ret.Add(currentToken);
                currentToken = null;
            }

            return ret;
        }
    }

    /// <summary>
    /// Represents the set of supported boolean operators
    /// </summary>
    public enum BooleanOperator
    {
        /// <summary>
        /// Represents an 'and' boolean operation
        /// </summary>
        And = (int)'&',
        /// <summary>
        /// Represents an 'or' boolean operation
        /// </summary>
        Or = (int)'|',
    }

    /// <summary>
    /// An enum representing a type of boolean expression token
    /// </summary>
    public enum BooleanExpressionTokenType
    {
        /// <summary>
        /// Represents a boolean variable
        /// </summary>
        Variable = 0,
        /// <summary>
        /// Represents the beginning of a logically grouped boolean expression
        /// </summary>
        GroupOpen = (int)'(',
        /// <summary>
        /// Represents the end of a logically grouped boolean expression
        /// </summary>
        GroupClose = (int)')',
        /// <summary>
        /// Represents an 'and' clause in a boolean expression
        /// </summary>
        And = (int)'&',
        /// <summary>
        /// Represents an 'or' clause in a boolean expression
        /// </summary>
        Or = (int)'|',
    }

    /// <summary>
    /// A class that represents a boolean expression token
    /// </summary>
    public class BooleanExpressionToken
    {
        /// <summary>
        /// The type of token
        /// </summary>
        public BooleanExpressionTokenType Type { get; set; }

        /// <summary>
        /// The value of the token as a string
        /// </summary>
        public string TokenValue { get; set; }
    }

    /// <summary>
    /// An interface that describes how to resolve boolean variables that can be either true or false
    /// </summary>
    public interface IVariableResolver
    {
        /// <summary>
        /// Implementations should provide a value of true or false for each variable specified.  Implementations can
        /// choose how to handle unknown variables either by throwing or returning a default value.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        bool ResolveBoolean(string variableName);
    }

    /// <summary>
    /// A class that can resolve a boolean variable based on a function.
    /// </summary>
    public class FuncVariableResolver : IVariableResolver
    {
        /// <summary>
        /// The function that knows how to resolve boolean variables
        /// </summary>
        public Func<string, bool> ResolverImpl { get; private set; }

        /// <summary>
        /// Creates a new variable resolver given an implementation as a function.
        /// </summary>
        /// <param name="resolverImpl"></param>
        public FuncVariableResolver(Func<string, bool> resolverImpl)
        {
            if (resolverImpl == null) throw new ArgumentNullException("resolverImpl cannot be null");
            this.ResolverImpl = resolverImpl;
        }

        /// <summary>
        /// Resolves the given variable using the wrapped function
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public bool ResolveBoolean(string variableName)
        {
            if (variableName == null) throw new ArgumentNullException("Variable name cannot be null");
            return ResolverImpl(variableName);
        }
    }

    internal class DictionaryVariableResolver : IVariableResolver
    {
        public Dictionary<string, bool> InnerDictionary { get; private set; }

        public DictionaryVariableResolver(Dictionary<string, bool> innerDictionary)
        {
            if (innerDictionary == null)
            {
                throw new ArgumentException("innerDictionary cannot be null");
            }
            this.InnerDictionary = innerDictionary;
        }

        public bool ResolveBoolean(string variableName)
        {
            bool val;
            if (InnerDictionary.TryGetValue(variableName, out val) == false)
            {
                throw new ArgumentException("Unknown variable '" + variableName + "'");
            }
            return val;
        }
    }

    /// <summary>
    /// An interface representing a node in a boolean expression that can either be true or false.
    /// </summary>
    public interface IBooleanExpressionNode
    {
        /// <summary>
        /// Evaluates the state of the node (true or false) given a variable resolver.
        /// </summary>
        /// <param name="resolver"></param>
        /// <returns></returns>
        bool Evaluate(IVariableResolver resolver);
    }

    /// <summary>
    /// A node in a boolean expression that represents a variable that can either be true or false.
    /// </summary>
    public class BooleanExpressionVariable : IBooleanExpressionNode
    {
        /// <summary>
        /// The name of the variable referenced by this node
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// Uses the given resolver to resolve the target boolean variable
        /// </summary>
        /// <param name="resolver">The object used to resolve boolean variables</param>
        /// <returns>the result of the resolver</returns>
        public bool Evaluate(IVariableResolver resolver)
        {
            bool val = resolver.ResolveBoolean(this.VariableName);
            return val;
        }
    }

    /// <summary>
    /// A class that represents a boolean expression that supports and, or, and grouping.
    /// </summary>
    public class BooleanExpression : IBooleanExpressionNode
    {
        /// <summary>
        /// The operands (variables or grouped child expressions) that make up this expression.
        /// </summary>
        public List<IBooleanExpressionNode> Operands { get; private set; }

        /// <summary>
        /// The operators to apply between each operand
        /// </summary>
        public List<BooleanOperator> Operators { get; private set; }


        /// <summary>
        /// Creates a new empty boolean expression
        /// </summary>
        public BooleanExpression()
        {
            Operands = new List<IBooleanExpressionNode>();
            Operators = new List<BooleanOperator>();
        }

        /// <summary>
        /// Evaluates the expression given a set of variable values
        /// </summary>
        /// <param name="variableValues">The value of variables that appear in the expression</param>
        /// <returns>True if the expression was true, false otherwise</returns>
        public bool Evaluate(Dictionary<string, bool> variableValues)
        {
            return Evaluate(new DictionaryVariableResolver(variableValues));
        }

        /// <summary>
        /// Evaluates the expression given a variable resolver.
        /// </summary>
        /// <param name="resolver">An object used to resolve variables that appear in the expression</param>
        /// <returns>True if the expression was true, false otherwise</returns>
        public bool Evaluate(IVariableResolver resolver)
        {
            if (Operands.Count == 0)
            {
                throw new ArgumentException("Nothing to evaluate");
            }

            if (Operators.Count != Operands.Count - 1)
            {
                throw new ArgumentException("Unexpected number of operators");
            }

            int operatorIndex = 0;
            foreach (var operand in Operands)
            {
                var currentValue = operand.Evaluate(resolver);

                if (operatorIndex <= Operators.Count - 1)
                {
                    var currentOperator = Operators[operatorIndex++];

                    if (currentValue == true && currentOperator == BooleanOperator.Or)
                    {
                        return true;
                    }
                    else if (currentValue == false && currentOperator == BooleanOperator.And)
                    {
                        return false;
                    }
                }
                else
                {
                    return currentValue;
                }
            }

            throw new ArgumentException("This should never happen :)");
        }
    }
}
