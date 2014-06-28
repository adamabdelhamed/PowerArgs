using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// An enum used to add type metadata to object path expression tokens
    /// </summary>
    public enum ObjectPathTokenType
    {
        /// <summary>
        /// Indicates the start of an index navigation for objects like arrays, lists, and dictionaries.  '['
        /// </summary>
        IndexerOpen,       
        /// <summary>
        /// Indicates the end of an index navigation for objects like arrays, lists, and dictionaries. ']'
        /// </summary>
        IndexerClose,     
        /// <summary>
        /// Indicates a property navigation for an object.
        /// </summary>
        NavigationElement,
        /// <summary>
        /// Indicates a property identifier
        /// </summary>
        Identifier,            
        /// <summary>
        /// Indicates whitespace
        /// </summary>
        Whitespace,       
        /// <summary>
        /// Indicates a string literal inside of double quotes
        /// </summary>
        StringLiteral,    
    }

    /// <summary>
    /// A token that is a part of an object path expression string
    /// </summary>
    public class ObjectPathToken : Token
    {
        /// <summary>
        /// Gets the type of token
        /// </summary>
        public ObjectPathTokenType TokenType { get; private set; }

        /// <summary>
        /// Creates an object path token
        /// </summary>
        /// <param name="initialValue">The initial value of the token</param>
        /// <param name="startIndex">The start index of the token in the source string</param>
        /// <param name="line">The line number that this token is on</param>
        /// <param name="col">The column within the line that this token is on</param>
        public ObjectPathToken(string initialValue, int startIndex, int line, int col) : base(initialValue, startIndex, line, col) { }

        /// <summary>
        /// A method that can determine which type of token the given value is
        /// </summary>
        /// <param name="token">The token to classify</param>
        /// <param name="previous">Previously classified tokens in the source string</param>
        /// <returns>The classified token</returns>
        public static ObjectPathToken TokenFactoryImpl(Token token, List<ObjectPathToken> previous)
        {
            var ret = token.As<ObjectPathToken>();
            if (ret.Value == "[")
            {
                ret.TokenType = ObjectPathTokenType.IndexerOpen;
            }
            else if (ret.Value == "]")
            {
                ret.TokenType = ObjectPathTokenType.IndexerClose;
            }
            else if (ret.Value == ".")
            {
                ret.TokenType = ObjectPathTokenType.NavigationElement;
            }
            else if (ret.Value.StartsWith("\"") && ret.Value.EndsWith("\"") && ret.Value.Length > 1)
            {
                ret.TokenType = ObjectPathTokenType.StringLiteral;
            }
            else if(string.IsNullOrWhiteSpace(ret.Value))
            {
                ret.TokenType = ObjectPathTokenType.Whitespace;
            }
            else
            {
                ret.TokenType = ObjectPathTokenType.Identifier;
            }
            return ret;
        }
    }

    /// <summary>
    /// An object that represents navigation into an object properties or indexed elements
    /// </summary>
    public class ObjectPathExpression
    {
        /// <summary>
        /// The path elements for this expression
        /// </summary>
        public List<IObjectPathElement> Elements { get; private set; }

        /// <summary>
        /// Create a path expression given a collection of path elements
        /// </summary>
        /// <param name="elements">The path elements</param>
        public ObjectPathExpression(IEnumerable<IObjectPathElement> elements)
        {
            this.Elements = elements.ToList();
        }

        /// <summary>
        /// Parses an object path expression from a string.
        /// </summary>
        /// <param name="expression">The expression text to parse</param>
        /// <returns>The parsed expression</returns>
        public static ObjectPathExpression Parse(string expression)
        {
            if (expression == null) throw new ArgumentNullException("path cannot be null");
            if (expression.Length == 0) throw new FormatException("Cannot parse empty string");

            Tokenizer<ObjectPathToken> tokenizer = new Tokenizer<ObjectPathToken>();
            tokenizer.TokenFactory = ObjectPathToken.TokenFactoryImpl;
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndInclude;
            tokenizer.DoubleQuoteBehavior = DoubleQuoteBehavior.IncludeQuotedTokensAsStringLiterals;
            tokenizer.Delimiters.Add("[");
            tokenizer.Delimiters.Add("]");
            tokenizer.Delimiters.Add(".");
            List<ObjectPathToken> tokens = tokenizer.Tokenize(expression);

            TokenReader<ObjectPathToken> reader = new TokenReader<ObjectPathToken>(tokens);

            List<IObjectPathElement> pathElements = new List<IObjectPathElement>();

            bool lastTokenWasNavigation = false;
            while (reader.CanAdvance(skipWhitespace: true))
            {
                var currentToken = reader.Advance(skipWhitespace: true);

                if (lastTokenWasNavigation == true && currentToken.TokenType == ObjectPathTokenType.IndexerOpen)
                {
                    throw new FormatException("Expected property, got '['" + " at " + currentToken.Position);
                }

                lastTokenWasNavigation = false;

                if(pathElements.Count == 0 && currentToken.TokenType == ObjectPathTokenType.NavigationElement)
                {
                    throw new FormatException("Expected property or index, got '" + currentToken.Value + "'" + " at " + currentToken.Position);
                }

                if (currentToken.TokenType == ObjectPathTokenType.IndexerClose ||
                    currentToken.TokenType == ObjectPathTokenType.StringLiteral)
                {
                    throw new FormatException("Expected property or index, got '" + currentToken.Value + "'" + " at " + currentToken.Position);
                }

                if (currentToken.TokenType == ObjectPathTokenType.IndexerOpen)
                {
                    // read index value
                    if (reader.TryAdvance(out currentToken,skipWhitespace: true) == false) throw new FormatException("Expected index value, got end of string");

                    if (currentToken.TokenType == ObjectPathTokenType.Identifier || currentToken.TokenType == ObjectPathTokenType.StringLiteral)
                    {
                        string indexValueText = currentToken.Value;

                        if(currentToken.TokenType == ObjectPathTokenType.StringLiteral)
                        {
                            indexValueText = indexValueText.Substring(1, indexValueText.Length - 2);
                        }

                        object indexValue;
                        int indexValueInt;
                        if (int.TryParse(indexValueText, out indexValueInt) == false)
                        {
                            indexValue = indexValueText;
                        }
                        else
                        {
                            indexValue = indexValueInt;
                        }

                        // read index close
                        if (reader.TryAdvance(out currentToken, skipWhitespace: true) == false) throw new FormatException("Expected ']', got end of string");
                        if (currentToken.TokenType != ObjectPathTokenType.IndexerClose) throw new FormatException("Expected ']', got '" + currentToken.Value + "' at " + currentToken.Position);

                        IndexerPathElement el = new IndexerPathElement(indexValue);
                        pathElements.Add(el);

                        if (reader.TryAdvance(out currentToken, skipWhitespace: true))
                        {
                            if (currentToken.TokenType != ObjectPathTokenType.NavigationElement) throw new FormatException("Expected '.', got '" + currentToken.Value + "' at " + currentToken.Position);
                            if (reader.CanAdvance(skipWhitespace: true) == false) throw new FormatException("Expected property, got end of string");
                            lastTokenWasNavigation = true;
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Unexpected token '" + currentToken.Value + "' at " + currentToken.Position);
                    }
                }
                else if(currentToken.TokenType == ObjectPathTokenType.Identifier)
                {
                    PropertyPathElement el = new PropertyPathElement(currentToken.Value);
                    pathElements.Add(el);
                }
                else if(currentToken.TokenType == ObjectPathTokenType.NavigationElement)
                {
                    // do nothing
                }
                else
                {
                    throw new ArgumentException("Unexpected token '" + currentToken.Value + "' at " + currentToken.Position);
                }
            }

            return new ObjectPathExpression(pathElements);
        }

        /// <summary>
        /// Evaluates the expression and returns the value
        /// </summary>
        /// <param name="root">the object to evaluate against</param>
        /// <returns>The result of the evaluation</returns>
        public object Evaluate(object root)
        {
            return EvaluateAndTrace(root).Last();
        }

        /// <summary>
        /// Evaluates the expression, returning the object that corresponds to each element in the path.
        /// </summary>
        /// <param name="root">the object to evaluate against</param>
        /// <returns>A list of object where each object corresponds to an element in the path</returns>
        public List<object> EvaluateAndTrace(object root)
        {
            if (root == null) throw new ArgumentNullException("root cannot be null");

            string rootPath = "root";
            string currentPath = rootPath;

            List<object> ret = new List<object>();

            var currentObject = root;

            ret.Add(currentObject);
            foreach (var pathElement in Elements)
            {
                rootPath += pathElement.ToString();
                if (currentObject == null)
                {
                    throw new NullReferenceException("Null reference at path: " + currentPath);
                }

                if (pathElement is PropertyPathElement)
                {
                    var propEl = pathElement as PropertyPathElement;
                    var propInfo = currentObject.GetType().GetProperty(propEl.PropertyName);

                    if(propInfo == null) throw new InvalidOperationException("Type "+currentObject.GetType().Name+" does not have a property called "+propEl.PropertyName);

                    currentObject = propInfo.GetValue(currentObject, null);
                    ret.Add(currentObject);
                }
                else if (pathElement is IndexerPathElement)
                {
                    var collectionEl = pathElement as IndexerPathElement;

                    if(currentObject.GetType().IsArray)
                    {
                        object[] arr = ((IEnumerable)currentObject).Cast<object>().ToArray();
                        currentObject = arr[(int)collectionEl.Index];
                    }
                    else if (currentObject is string)
                    {
                        var objString = (string)currentObject;
                        currentObject = objString[(int)collectionEl.Index];
                    }
                    else
                    {
                        var indexerProperty = collectionEl.FindMatchingProperty(currentObject);
                        if (indexerProperty == null)
                        {
                            throw new InvalidOperationException("Type " + currentObject.GetType().Name + " does not have a supported indexer property of type " + collectionEl.Index.GetType());
                        }
                        currentObject = indexerProperty.GetValue(currentObject, new object[] { collectionEl.Index });
                    }
                    ret.Add(currentObject);
                }
                else
                {
                    throw new NotImplementedException("Unknown path element type: " + pathElement.GetType().FullName);
                }
            }

            return ret;
        }
    }

    /// <summary>
    /// An object that represents a path element
    /// </summary>
    public interface IObjectPathElement { }

    /// <summary>
    /// A path element that represents an object's property
    /// </summary>
    public class PropertyPathElement : IObjectPathElement
    {

        /// <summary>
        /// Gets the name of the property
        /// </summary>
        public string PropertyName { get; private set; }

        /// <summary>
        /// Creates a property path element given a property name
        /// </summary>
        /// <param name="propertyName">the name of the property</param>
        public PropertyPathElement(string propertyName)
        {
            if (propertyName == null) throw new ArgumentNullException("info cannot be null");
            this.PropertyName = propertyName;
        }

        /// <summary>
        /// Returns the property name
        /// </summary>
        /// <returns>the property name</returns>
        public override string ToString()
        {
            return PropertyName;
        }
    }

    /// <summary>
    /// A path element that represents an index navigation
    /// </summary>
    public class IndexerPathElement : IObjectPathElement
    {
        private object _index;

        /// <summary>
        /// The indexer value, either a literal string or an integer
        /// </summary>
        public object Index
        {
            get
            {
                return _index;
            }
            set
            {
                if(value is int == false && value is string == false)
                {
                    throw new ArgumentException("Value must be an integer or a string");
                }
                this._index = value;
            }
        }
        
        /// <summary>
        /// Creates an indexer element given an indexer value
        /// </summary>
        /// <param name="index"></param>
        public IndexerPathElement(object index)
        {
            this.Index = index;
        }

        /// <summary>
        /// Finds the matching property info that represents an indexer property (do not use for strings or arrays).
        /// </summary>
        /// <param name="target">The object to search</param>
        /// <returns>The matching property</returns>
        public PropertyInfo FindMatchingProperty(object target)
        {
            var match = from p in target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        where p.GetIndexParameters().Length == 1 &&
                              p.GetIndexParameters()[0].ParameterType == this.Index.GetType()
                        select p;
            if(match.Count() > 1)
            {
                throw new NotSupportedException("There are multiple indexer properties that match the target");
            }
            return match.SingleOrDefault();
        }

        /// <summary>
        /// returns '[' + the index value + ']'
        /// </summary>
        /// <returns>'[' + the index value + ']'</returns>
        public override string ToString()
        {
            if (Index is int)
            {
                return "[" + Index + "]";
            }
            else
            {
                return "[" + '"' + Index + '"' + "]";
            }
        }
    }
}
