using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// An enum defining the types of tokens that can appear in a templated document
    /// </summary>
    public enum DocumentTokenType
    {
        /// <summary>
        /// Indicates the Key of a replacement tag.  Example: 'each' in '{{each foo in bar}}'
        /// </summary>
        ReplacementKey,
        /// <summary>
        /// Indicates a parameter in a replacement tag.  Example: 'foo', 'in', and 'bar' in '{{each foo in bar}}'
        /// </summary>
        ReplacementParameter,
        /// <summary>
        /// Text that is not transformed by the document renderer
        /// </summary>
        PlainText,
        /// <summary>
        /// Indicates the beginning of a replacement tag '{{'.
        /// </summary>
        BeginReplacementSegment,
        /// <summary>
        /// Indicates the end of a replacement segment '}}'
        /// </summary>
        EndReplacementSegment,
        /// <summary>
        /// Indicates the beginning of a termination segment '!{{'
        /// </summary>
        BeginTerminateReplacementSegment,
        /// <summary>
        /// Indicates that a replacement segment has no body and this is the end of the segment '!}}'
        /// </summary>
        QuickTerminateReplacementSegment,
    }

    /// <summary>
    /// A class that represents a token in a templated document
    /// </summary>
    public class DocumentToken : Token
    {
        /// <summary>
        /// The type of this token
        /// </summary>
        public DocumentTokenType TokenType { get; set; }

        /// <summary>
        /// Creates a new document token
        /// </summary>
        /// <param name="initialValue">The initial value of the token</param>
        /// <param name="startIndex">the zero based character index of this token in a document template</param>
        /// <param name="line">The line number of this token in a document template (starts at 1)</param>
        /// <param name="col">The column number of this token in a document template (starts at 1)</param>
        public DocumentToken(string initialValue, int startIndex, int line, int col) : base(initialValue, startIndex, line, col) { }

        /// <summary>
        /// Gets the constant string value of a given token type.  This method will throw an exception if the
        /// type provided does not map to a constant string value.
        /// </summary>
        /// <param name="type">The type to lookup</param>
        /// <returns>The literal string value expected of a token of the given type</returns>
        public static string GetTokenTypeValue(DocumentTokenType type)
        {
            string ret;
            if(TryGetTokenTypeValue(type, out ret) == false)
            {
                throw new ArgumentException("The type '"+type+"' does not have a constant string value");
            }
            return ret;
        }

        /// <summary>
        /// Tries to get the constant string value of a given token type.  This method will return false for types that don't
        /// map to a constant string value.
        /// </summary>
        /// <param name="type">The type to lookup</param>
        /// <param name="val">The literal string value expected of a token of the given type</param>
        /// <returns>true if 'val' was populated, false otherwise</returns>
        public static bool TryGetTokenTypeValue(DocumentTokenType type, out string val)
        {
            if (type == DocumentTokenType.BeginReplacementSegment)
            {
                val = "{{";
            }
            else if(type == DocumentTokenType.EndReplacementSegment)
            {
                val = "}}";
            }
            else if(type == DocumentTokenType.QuickTerminateReplacementSegment)
            {
                val = "!}}";
            }
            else if(type == DocumentTokenType.BeginTerminateReplacementSegment)
            {
                val = "!{{";
            }
            else
            {
                val = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to parse a literal string value to a well known document token type.
        /// </summary>
        /// <param name="stringVal">The value to try to parse</param>
        /// <param name="type">The reference to populate if parsing is successful</param>
        /// <returns>True if the string could be successfully mapped to a DocumentTokenType, false otherwise</returns>
        public static bool TryParseDocumentTokenType(string stringVal, out DocumentTokenType type)
        {
            if (stringVal == "{{")
            {
                type = DocumentTokenType.BeginReplacementSegment;
            }
            else if (stringVal == "}}")
            {
                type = DocumentTokenType.EndReplacementSegment;
            }
            else if (stringVal == "!}}")
            {
                type = DocumentTokenType.QuickTerminateReplacementSegment;
            }
            else if (stringVal == "!{{")
            {
                type = DocumentTokenType.BeginTerminateReplacementSegment;
            }
            else
            {
                type = default(DocumentTokenType);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tokenizes the given text into a list of DocumentToken objects.
        /// </summary>
        /// <param name="text">The text to tokenize</param>
        /// <param name="sourceLocation">A string describing the source of the text.  This could be a text file path or some other identifier.</param>
        /// <returns>A list of tokens</returns>
        public static List<DocumentToken> Tokenize(string text, string sourceLocation)
        {
            Tokenizer<DocumentToken> tokenizer = new Tokenizer<DocumentToken>();
            tokenizer.SourceFileLocation = sourceLocation;
            tokenizer.Delimiters.AddRange(DocumentToken.Delimiters);
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndInclude;
            tokenizer.TokenFactory = DocumentToken.TokenFactoryImpl;
            List<DocumentToken> tokens = tokenizer.Tokenize(text);
            List<DocumentToken> filtered = RemoveLinesThatOnlyContainReplacements(tokens);
            return filtered;
        }

        private static List<string> Delimiters
        {
            get
            {
                List<string> ret = new List<string>();
                foreach (DocumentTokenType type in Enum.GetValues(typeof(DocumentTokenType)))
                {
                    string textVal;
                    if (TryGetTokenTypeValue(type, out textVal))
                    {
                        ret.Add(textVal);
                    }
                }

                return ret;
            }
        }

        private static DocumentToken TokenFactoryImpl(Token token, List<DocumentToken> previous)
        {
            var utToken = token.As<DocumentToken>();

            DocumentTokenType? previousNonWhitespaceTokenType = null;

            for(var i = previous.Count-1;i >= 0; i--)
            {
                var current = previous[i];
                if(string.IsNullOrWhiteSpace(current.Value) == false)
                {
                    previousNonWhitespaceTokenType = current.TokenType;
                    break;
                }
            }

            DocumentTokenType constantValueType;
            if(TryParseDocumentTokenType(token.Value, out constantValueType))
            {
                utToken.TokenType = constantValueType;
            }
            else if (previousNonWhitespaceTokenType.HasValue && (previousNonWhitespaceTokenType.Value == DocumentTokenType.BeginReplacementSegment || previousNonWhitespaceTokenType.Value == DocumentTokenType.BeginTerminateReplacementSegment))
            {
                utToken.TokenType = DocumentTokenType.ReplacementKey;
            }
            else if (previousNonWhitespaceTokenType.HasValue && (previousNonWhitespaceTokenType.Value == DocumentTokenType.ReplacementKey || previousNonWhitespaceTokenType.Value == DocumentTokenType.ReplacementParameter))
            {
                utToken.TokenType = DocumentTokenType.ReplacementParameter;
            }
            else
            {
                utToken.TokenType = DocumentTokenType.PlainText;
            }

            return utToken;
        }

        private static List<DocumentToken> RemoveLinesThatOnlyContainReplacements(List<DocumentToken> tokens)
        {
            var currentLine = 1;
            int numContentTokensOnCurrentLine = 0;
            int numReplacementTokensOnCurrentLine = 0;

            List<DocumentToken> filtered = new List<DocumentToken>();
            DocumentToken lastToken = null;
            foreach (var token in tokens)
            {
                lastToken = token;
                if (token.Line != currentLine)
                {
                    currentLine = token.Line;
                    FilterOutNewlinesIfNeeded(numContentTokensOnCurrentLine, numReplacementTokensOnCurrentLine, filtered);
                    numReplacementTokensOnCurrentLine = 0;
                    numContentTokensOnCurrentLine = 0;
                }

                if (string.IsNullOrWhiteSpace(token.Value))
                {
                    // do nothing
                }
                else if (IsReplacementToken(token))
                {
                    numReplacementTokensOnCurrentLine++;
                }
                else
                {
                    numContentTokensOnCurrentLine++;
                }

                filtered.Add(token);
            }

            if (lastToken != null)
            {
                currentLine = lastToken.Line;
                FilterOutNewlinesIfNeeded(numContentTokensOnCurrentLine, numReplacementTokensOnCurrentLine, filtered);
                numReplacementTokensOnCurrentLine = 0;
                numContentTokensOnCurrentLine = 0;
            }

            return filtered;
        }

        private static void FilterOutNewlinesIfNeeded(int numContentTokensOnCurrentLine, int numReplacementTokensOnCurrentLine, List<DocumentToken> filtered)
        {
            if (numContentTokensOnCurrentLine == 0 && numReplacementTokensOnCurrentLine > 0)
            {
                if (filtered.Count >= 2 && filtered[filtered.Count - 2].Value == "\r" && filtered[filtered.Count - 1].Value == "\n")
                {
                    // this line only had replacements so remove the trailing carriage return and newline
                    filtered.RemoveAt(filtered.Count - 1);
                    filtered.RemoveAt(filtered.Count - 1);
                    //Console.WriteLine("removed CR+NL as 2 tokens");
                }
                if (filtered.Count >= 1 && filtered[filtered.Count - 1].Value == "\r\n")
                {
                    // this line only had replacements so remove the trailing carriage return and newline (in the same token)
                    filtered.RemoveAt(filtered.Count - 1);
                    //Console.WriteLine("removed CR+NL as 1 token");
                }
                else if (filtered.Count >= 1 && filtered[filtered.Count - 1].Value == "\n")
                {
                    // this line only had replacements so remove the trailing newline
                    filtered.RemoveAt(filtered.Count - 1);
                    //Console.WriteLine("removed NL token");
                }
            }
        }

        private static bool IsReplacementToken(DocumentToken token)
        {
            return token.TokenType == DocumentTokenType.BeginReplacementSegment ||
                    token.TokenType == DocumentTokenType.BeginTerminateReplacementSegment ||
                    token.TokenType == DocumentTokenType.EndReplacementSegment ||
                    token.TokenType == DocumentTokenType.QuickTerminateReplacementSegment ||
                    token.TokenType == DocumentTokenType.ReplacementKey ||
                    token.TokenType == DocumentTokenType.ReplacementParameter;
        }
    }
}
