using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs
{
    public enum ObjectPathTokenType
    {
        IndexerOpen,       // '['
        IndexerClose,      // ']'
        NavigationElement, // '.'
        Text,              // Other text
        Whitespace,        // whitespace
        StringLiteral,     // text inside double quotes
    }

    public class ObjectPathToken : Token
    {
        public ObjectPathTokenType TokenType { get; set; }

        public ObjectPathToken(string initialValue, int startIndex, int line, int col) : base(initialValue, startIndex, line, col) { }

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
                ret.TokenType = ObjectPathTokenType.Text;
            }
            return ret;
        }
    }

    public class ObjectPathExpression
    {
        public List<IObjectPathElement> Elements { get; private set; }

        public ObjectPathExpression(IEnumerable<IObjectPathElement> elements)
        {
            this.Elements = elements.ToList();
        }

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

                    if (currentToken.TokenType == ObjectPathTokenType.Text || currentToken.TokenType == ObjectPathTokenType.StringLiteral)
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
                else if(currentToken.TokenType == ObjectPathTokenType.Text)
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


        public object Evaluate(object root)
        {
            return EvaluateAndTrace(root).Last();
        }

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

    public interface IObjectPathElement { }

    public class PropertyPathElement : IObjectPathElement
    {
        public string PropertyName { get; private set; }

        public PropertyPathElement(string propertyName)
        {
            if (propertyName == null) throw new ArgumentNullException("info cannot be null");
            this.PropertyName = propertyName;
        }

        public override string ToString()
        {
            return PropertyName;
        }
    }

    public class IndexerPathElement : IObjectPathElement
    {
        public object _index;
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
        
        public IndexerPathElement(object index)
        {
            this.Index = index;
        }

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
