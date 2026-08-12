using System;
using System.Collections.Generic;

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
        /// <param name="startIndex">the zero based character index of this token in a document template</param>
        /// <param name="line">The line number of this token in a document template (starts at 1)</param>
        /// <param name="col">The column number of this token in a document template (starts at 1)</param>
        public DocumentToken(int startIndex, int line, int col) : base(startIndex, line, col) { }

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
            if (text == null) throw new ArgumentNullException(nameof(text));

            var tokens = Scan(text, sourceLocation);
            AssignTokenTypes(tokens);
            return RemoveLinesThatOnlyContainReplacements(tokens);
        }

        private static List<DocumentToken> Scan(string text, string sourceLocation)
        {
            var tokens = new List<DocumentToken>();
            var index = 0;
            var line = 1;
            var column = 1;

            while (index < text.Length)
            {
                var tokenStart = index;
                var tokenLine = line;
                var tokenColumn = column;
                var delimiterLength = GetDelimiterLength(text, index);

                if (delimiterLength > 0)
                {
                    index += delimiterLength;
                    column += delimiterLength;
                }
                else if (char.IsWhiteSpace(text[index]))
                {
                    var current = text[index++];
                    if (current == '\n')
                    {
                        line++;
                        column = 1;
                    }
                    else column++;
                }
                else
                {
                    do
                    {
                        index++;
                        column++;
                    }
                    while (index < text.Length && char.IsWhiteSpace(text[index]) == false && GetDelimiterLength(text, index) == 0);
                }

                tokens.Add(new DocumentToken(tokenStart, tokenLine, tokenColumn) { Value = text.Substring(tokenStart, index - tokenStart), SourceFileLocation = sourceLocation });
            }

            return tokens;
        }

        private static int GetDelimiterLength(string text, int index)
        {
            if (index + 2 < text.Length && text[index] == '!' && text[index + 1] == '{' && text[index + 2] == '{') return 3;
            if (index + 2 < text.Length && text[index] == '!' && text[index + 1] == '}' && text[index + 2] == '}') return 3;
            if (index + 1 < text.Length && text[index] == '{' && text[index + 1] == '{') return 2;
            if (index + 1 < text.Length && text[index] == '}' && text[index + 1] == '}') return 2;
            return 0;
        }

        private static void AssignTokenTypes(List<DocumentToken> tokens)
        {
            DocumentTokenType? previousNonWhitespaceTokenType = null;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                DocumentTokenType constantValueType;
                if (TryParseDocumentTokenType(token.Value, out constantValueType)) token.TokenType = constantValueType;
                else if (previousNonWhitespaceTokenType.HasValue && (previousNonWhitespaceTokenType.Value == DocumentTokenType.BeginReplacementSegment || previousNonWhitespaceTokenType.Value == DocumentTokenType.BeginTerminateReplacementSegment)) token.TokenType = DocumentTokenType.ReplacementKey;
                else if (previousNonWhitespaceTokenType.HasValue && (previousNonWhitespaceTokenType.Value == DocumentTokenType.ReplacementKey || previousNonWhitespaceTokenType.Value == DocumentTokenType.ReplacementParameter)) token.TokenType = DocumentTokenType.ReplacementParameter;
                else token.TokenType = DocumentTokenType.PlainText;

                if (string.IsNullOrWhiteSpace(token.Value) == false) previousNonWhitespaceTokenType = token.TokenType;
            }
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
