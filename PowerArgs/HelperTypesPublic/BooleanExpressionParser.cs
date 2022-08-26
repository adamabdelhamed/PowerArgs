using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// A simple boolean expression parser that supports and '&amp;', or '|', and grouping via parentheses.
    /// </summary>
    [ArgReviverType]
    public static class BooleanExpressionParser
    {
        /// <summary>
        /// Parses the given boolean expression which can be made up of variables and boolean operators (and '&amp;' and or '|') grouped by parentheses.
        /// </summary>
        /// <param name="expressionText">The expression to parse</param>
        /// <returns>The parsed expression</returns>
        public static IBooleanExpression Parse(string expressionText)
        {
            List<Token> tokens = Tokenize(expressionText);
            IBooleanExpression tree = BuildTree(tokens);
            return tree;
        }

        /// <summary>
        /// A reviver that makes boolean expressions specificable on the command line
        /// </summary>
        /// <param name="key">not used</param>
        /// <param name="val">the expression text</param>
        /// <returns></returns>
        [ArgReviver]
        public static IBooleanExpression Revive(string key, string val)
        {
            try
            {
                return Parse(val);
            }
            catch(Exception ex)
            {
                throw new ValidationArgException(string.Format("Unable to parse expression '{0}'", val), ex);
            }
        }


        private static IBooleanExpression BuildTree(List<Token> tokens)
        {
            BooleanExpressionGroup defaultGroup = new BooleanExpressionGroup();

            bool not = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (token.Value == ")" ||
                    (defaultGroup.Operands.Count == 0 && (token.Value == "&" || token.Value == "|")) ||
                    (i == tokens.Count - 1 && (token.Value == "&" || token.Value == "|")))
                {
                    throw new ArgumentException("Unexpected token '" + token.Value + "'");
                }

                if (token.Value == "(")
                {
                    int numOpen = 1;
                    List<Token> groupedExpression = new List<Token>();
                    for (int j = i + 1; j < tokens.Count; j++)
                    {
                        var innerToken = tokens[j];
                        if (innerToken.Value == ")" && numOpen == 1)
                        {
                            numOpen = 0;
                            i = j;
                            break;
                        }
                        else if (innerToken.Value == ")")
                        {
                            numOpen--;
                        }
                        else if (innerToken.Value == "(")
                        {
                            numOpen++;
                        }

                        groupedExpression.Add(innerToken);
                    }

                    if (numOpen > 0)
                    {
                        throw new ArgumentException("You are missing at least 1 closing parenthesis");
                    }

                    if (groupedExpression.Count == 0)
                    {
                        throw new ArgumentException("You have an empty set of parenthesis");
                    }

                    var group = BuildTree(groupedExpression);
                    group.Not = not;
                    defaultGroup.Operands.Add(group);
                    not = false;
                }
                else if (token.Value == "&" || token.Value == "|")
                {
                    if(not)
                    {
                        throw new ArgumentException("You cannot have an operator '&' or '|' after a '!'");
                    }

                    if (defaultGroup.Operators.Count >= defaultGroup.Operands.Count)
                    {
                        throw new ArgumentException("You cannot have two consecutive operators '&&' or '||', use '&' or '|'");
                    }
                    defaultGroup.Operators.Add(token.Value == "&" ? BooleanOperator.And : BooleanOperator.Or);
                }
                else if(token.Value == "!")
                {
                    if (not)
                    {
                        throw new ArgumentException("You cannot have two consecutive '!' operators");
                    }
                    else
                    {
                        not = true;
                    }
                }
                else
                {
                    if (defaultGroup.Operands.Count != defaultGroup.Operators.Count)
                    {
                        throw new ArgumentException("Expected an operator, got variable: " + token.Value);
                    }

                    defaultGroup.Operands.Add(new BooleanVariable() { VariableName = token.Value, Not = not });
                    not = false;
                }
            }

            if(not == true)
            {
                throw new ArgumentException("Unexpected token '!' at end of expression");
            }

            if (defaultGroup.Operands.Count == 1)
            {
                return defaultGroup.Operands[0];
            }
            else
            {
                return defaultGroup;
            }
        }

        private static BooleanExpressionTokenizer tokenizer = new BooleanExpressionTokenizer();
        private static List<Token> Tokenize(string expressionText)
        {
            return tokenizer.Tokenize(expressionText);
        }

        private class BooleanExpressionTokenizer : Tokenizer<Token>
        {
            public BooleanExpressionTokenizer()
            {
                Delimiters.AddRange(new string[] { "(", ")", "&", "|", "!" });
                WhitespaceBehavior = WhitespaceBehavior.DelimitAndExclude;
                DoubleQuoteBehavior = DoubleQuoteBehavior.IncludeQuotedTokensAsStringLiterals;
            }
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
        And = '&',
        /// <summary>
        /// Represents an 'or' boolean operation
        /// </summary>
        Or = '|',
    }

 

    /// <summary>
    /// An interface that describes how to resolve boolean variables that can be either true or false
    /// </summary>
    public interface IBooleanVariableResolver
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
    public class FuncBooleanVariableResolver : IBooleanVariableResolver
    {
        /// <summary>
        /// The function that knows how to resolve boolean variables
        /// </summary>
        public Func<string, bool> ResolverImpl { get; private set; }

        /// <summary>
        /// Creates a new variable resolver given an implementation as a function.
        /// </summary>
        /// <param name="resolverImpl"></param>
        public FuncBooleanVariableResolver(Func<string, bool> resolverImpl)
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

    internal class DictionaryBooleanVariableResolver : IBooleanVariableResolver
    {
        public Dictionary<string, bool> InnerDictionary { get; private set; }

        public DictionaryBooleanVariableResolver(Dictionary<string, bool> innerDictionary)
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
    public interface IBooleanExpression
    {
        /// <summary>
        /// Gets the names of all the variables that exist within the expression
        /// </summary>
        IEnumerable<string> VariableNames { get; }
        /// <summary>
        /// Evaluates the state of the node (true or false) given a variable resolver.
        /// </summary>
        /// <param name="resolver">the object to use to resolve boolean variables</param>
        /// <returns>the result of the expression, true or false</returns>
        bool Evaluate(IBooleanVariableResolver resolver);

        /// <summary>
        /// Evaluates the state of the node (true or false) given a set of variable values.
        /// </summary>
        /// <param name="variableValues">The current state of variables</param>
        /// <returns>the result of the expression, true or false</returns>
        bool Evaluate(Dictionary<string, bool> variableValues);

        /// <summary>
        /// Gets or sets a flag indicating that the expression should be negated
        /// </summary>
        bool Not { get; set; }
    }

    /// <summary>
    /// A node in a boolean expression that represents a variable that can either be true or false.
    /// </summary>
    public class BooleanVariable : IBooleanExpression
    {
        /// <summary>
        /// Gets or sets a flag indicating that the variable's value should be negated
        /// </summary>
        public bool Not { get; set; }

        /// <summary>
        /// The name of the variable referenced by this node
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// Gets the names of all the variables that exist within the expression
        /// </summary>
        public IEnumerable<string> VariableNames => new string[] { VariableName };

        /// <summary>
        /// Uses the given resolver to resolve the target boolean variable
        /// </summary>
        /// <param name="resolver">The object used to resolve boolean variables</param>
        /// <returns>the result of the resolver</returns>
        public bool Evaluate(IBooleanVariableResolver resolver)
        {
            bool val = resolver.ResolveBoolean(this.VariableName);
            return Not ? !val : val;
        }

        /// <summary>
        /// Evaluates the expression given a set of variable values
        /// </summary>
        /// <param name="variableValues">The value of variables that appear in the expression</param>
        /// <returns>True if the expression was true, false otherwise</returns>
        public bool Evaluate(Dictionary<string, bool> variableValues)
        {
            return Evaluate(new DictionaryBooleanVariableResolver(variableValues));
        }

        /// <summary>
        /// Gets a string representation of the variable
        /// </summary>
        /// <returns>a string representation of the variable</returns>
        public override string ToString()
        {
            var ret = "";
            if (Not) ret += "!";
            ret += VariableName;
            return ret;
        }
    }

    /// <summary>
    /// A class that represents a boolean expression that supports and, or, and grouping.
    /// </summary>
    public class BooleanExpressionGroup : IBooleanExpression
    {
        /// <summary>
        /// Gets or sets a flag indicating that the expression should be negated
        /// </summary>
        public bool Not { get; set; }

        /// <summary>
        /// The operands (variables or grouped child expressions) that make up this expression.
        /// </summary>
        public List<IBooleanExpression> Operands { get; private set; }

        /// <summary>
        /// Gets the names of all the variables that exist within the expression
        /// </summary>
        public IEnumerable<string> VariableNames
        {
            get
            {
                var ret = new List<string>();
                foreach(var exp in Operands)
                {
                    ret.AddRange(exp.VariableNames);
                }
                return ret.Distinct();
            }
        }

        /// <summary>
        /// The operators to apply between each operand
        /// </summary>
        public List<BooleanOperator> Operators { get; private set; }


        /// <summary>
        /// Creates a new empty boolean expression
        /// </summary>
        public BooleanExpressionGroup()
        {
            Operands = new List<IBooleanExpression>();
            Operators = new List<BooleanOperator>();
        }

        /// <summary>
        /// Evaluates the expression given a set of variable values
        /// </summary>
        /// <param name="variableValues">The value of variables that appear in the expression</param>
        /// <returns>True if the expression was true, false otherwise</returns>
        public bool Evaluate(Dictionary<string, bool> variableValues)
        {
            return Evaluate(new DictionaryBooleanVariableResolver(variableValues));
        }

        /// <summary>
        /// Evaluates the expression given a variable resolver.
        /// </summary>
        /// <param name="resolver">An object used to resolve variables that appear in the expression</param>
        /// <returns>True if the expression was true, false otherwise</returns>
        public bool Evaluate(IBooleanVariableResolver resolver)
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
                        return Not ? false : true;
                    }
                    else if (currentValue == false && currentOperator == BooleanOperator.And)
                    {
                        return Not ? true : false;
                    }
                }
                else
                {
                    return Not ? !currentValue : currentValue;
                }
            }

            throw new ArgumentException("This should never happen :)");
        }

        /// <summary>
        /// Gets a string representation of the variable
        /// </summary>
        /// <returns>a string representation of the variable</returns>
        public override string ToString()
        {
            if (Operands.Count == 0)
            {
                return "empty expression";
            }

            if (Operators.Count != Operands.Count - 1)
            {
                return "Unexpected number of operators";
            }

            var ret = "";
            if (Not) ret += "!";
            ret += "(";

            for (int i = 0; i < Operands.Count; i++)
            {
                var operand = Operands[i];
                ret += operand.ToString();

                if(i < Operators.Count)
                {
                    ret += " " +((char) Operators[i]) + " ";
                }

            }
            ret += ")";
            return ret;
        }
    }
}
