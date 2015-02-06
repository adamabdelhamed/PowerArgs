using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// An exception that will be thrown if there was an error while tokenizing a string.
    /// </summary>
    public class TokenizerException : Exception
    {
        /// <summary>
        /// Creates a new tokenizer exception given a message
        /// </summary>
        /// <param name="message">The exception message</param>
        public TokenizerException(string message) : base(message) { }
    }

    /// <summary>
    /// A base token class that represents a substring from a document.  The location in the source
    /// document is tracked along with the substring so that code that processes the token can indicate
    /// where problems are to the user who supplied the document if needed.
    /// </summary>
    public class Token
    {
        /// <summary>
        /// Gets the value of the token
        /// </summary>
        public string Value { get; internal set; }
        
        /// <summary>
        /// Gets the zero based start index of this token in the document
        /// </summary>
        public int StartIndex { get; private set; }

        /// <summary>
        /// Gets a string that represents the source file of this document.  It does not need to be a file name, but it usually is.
        /// </summary>
        public string SourceFileLocation { get; set; }

        /// <summary>
        /// Gets the end index of the token in the document
        /// </summary>
        public int EndIndex
        {
            get
            {
                return StartIndex + Value.Length;
            }
        }

        /// <summary>
        /// Gets the 1 based line number of the token in the document
        /// </summary>
        public int Line { get; private set; }
        
        /// <summary>
        /// Gets the 1 based index of the token within it's line
        /// </summary>
        public int Column { get; private set; }

        internal List<int> ExplicitNonDelimiterCharacters { get; private set; }

        /// <summary>
        /// Gets a string that represents the position of this token in the source document. 
        /// </summary>
        public string Position
        {
            get
            {
                return string.Format("Line '{0}', column '{1}', source '{2}'", Line, Column, SourceFileLocation);
            }

        }

        /// <summary>
        /// Creates a token given an initial value, a start index, a line number, and a column number
        /// </summary>
        /// <param name="initialValue">The initial value of a token.  You can append to the token later</param>
        /// <param name="startIndex">the zero based start index of this token in the document</param>
        /// <param name="line">the 1 based line number of the token in the document</param>
        /// <param name="col">the 1 based index of the token within it's line</param>
        public Token(string initialValue, int startIndex, int line, int col)
        {
            if (startIndex < 0)
            {
                throw new ArgumentException("token startIndex cannot be 0");
            }

            ExplicitNonDelimiterCharacters = new List<int>();
            Value = initialValue;
            StartIndex = startIndex;
            Line = line;
            Column = col;
        }

        internal void SetLastCharacterAsExplicitNonDelimiter()
        {
            if(this.Value.Length == 0)
            {
                throw new InvalidOperationException("This token has no value");
            }
            ExplicitNonDelimiterCharacters.Add(this.Value.Length - 1);
        }

        internal bool EndsWithDelimiter(string delimiter)
        {
            if(this.Value.EndsWith(delimiter) == false)
            {
                return false;
            }

            var firstTokenValue = this.Value.Substring(0, this.Value.Length - delimiter.Length);

            for(var i = firstTokenValue.Length; i < this.Value.Length; i++)
            {
                if(ExplicitNonDelimiterCharacters.Contains(i))
                {
                    return false; // TODO - P0 - no code coverage for this case
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a string representation of the token, along with position info
        /// </summary>
        /// <returns>a string representation of the token, along with position info</returns>
        public override string ToString()
        {
            return "---> '" + Value + "'<--- - " + Position;
        }

        /// <summary>
        /// Creates a new instance of the strongly typed token and copies all of the base token's properties to the new value
        /// </summary>
        /// <typeparam name="T">The type of the derived token.</typeparam>
        /// <returns>The strongly typed token</returns>
        public T As<T>() where T : Token
        {
            var ret = (T)Activator.CreateInstance(typeof(T), this.Value, this.StartIndex, this.Line, this.Column);
            ret.SourceFileLocation = this.SourceFileLocation;
            return ret;
        }
    }

    /// <summary>
    /// An enum describing the different ways the tokenizer can handle whitespace
    /// </summary>
    public enum WhitespaceBehavior
    {
        /// <summary>
        /// Treats whitespace as a delimiter and includes the whitespace tokens in the output list of tokens
        /// </summary>
        DelimitAndInclude,
        /// <summary>
        /// Treats whitespace as a delimiter, but excludes the whitespace tokens from the output list of tokens
        /// </summary>
        DelimitAndExclude,
        /// <summary>
        /// Includes whitespace in the output and does not treat it as a delimiter
        /// </summary>
        Include,
    }

    /// <summary>
    /// An enum describing the different ways the tokenizer can handle double quotes
    /// </summary>
    public enum DoubleQuoteBehavior
    {
        /// <summary>
        /// No special handling.  Double quotes will be treated like any normal character.  You can include the double quote in the delimiters list.
        /// </summary>
        NoSpecialHandling,
        /// <summary>
        /// Treat values within double quotes as string literals.
        /// </summary>
        IncludeQuotedTokensAsStringLiterals,
    }

    /// <summary>
    /// A general purpose string tokenizer
    /// </summary>
    /// <typeparam name="T">The type of tokens that this tokenizer should output</typeparam>
    public class Tokenizer<T> where T : Token
    {
        /// <summary>
        /// strings to treat as delimiters.  Delimiters with longer lengths will take preference over
        /// those with shorter lengths.  For example if you add delimiters '{{' and '{' and the document
        /// contains '{{Hello' then you'll get 2 tokens, first '{{', then 'hello'
        /// </summary>
        public List<string> Delimiters { get; private set; }

        /// <summary>
        /// A function that given a plain token can transform it into the strongly typed token
        /// </summary>
        public Func<Token, List<T>, T> TokenFactory { get; set; }

        /// <summary>
        /// Gets or sets the option that describes how whitespace should be treated.
        /// </summary>
        public WhitespaceBehavior WhitespaceBehavior { get; set; }

        /// <summary>
        /// Gets or sets the option that describes how double quotes should be treated.
        /// </summary>
        public DoubleQuoteBehavior DoubleQuoteBehavior { get; set; }

        /// <summary>
        /// An escape sequence identifier.  By default it is '\'
        /// </summary>
        public char EscapeSequenceIndicator { get; set; }

        /// <summary>
        /// A string that describes the source location for the given document
        /// </summary>
        public string SourceFileLocation { get; set; }
        
        private bool insideStringLiteral = false;
        
        /// <summary>
        /// Creates a new tokenizer
        /// </summary>
        public Tokenizer()
        {
            this.Delimiters = new List<string>();
            this.SourceFileLocation = "Not source location specified";
            if (typeof(T) == typeof(Token))
            {
                this.TokenFactory = (token, previousTokens) => (T)token;
            }
            else
            {
                this.TokenFactory = (token, previousTokens) => token.As<T>();
            }

            this.EscapeSequenceIndicator = '\\';
        }

        /// <summary>
        /// Tokenizes the given string into a list of tokens
        /// </summary>
        /// <param name="input">The string to tokenize</param>
        /// <returns>The list of tokens</returns>
        public List<T> Tokenize(string input)
        {
            List<T> tokens = new List<T>();
            Token currentToken = null;

            insideStringLiteral = false;

            char currentCharacter;
            char? nextCharacter;
            int currentIndex = -1;
            int currentLine = 1;
            int currentColumn = 0;
            while (TryReadCharacter(input, ref currentIndex, out currentCharacter))
            {
                char peeked;
                if(TryPeekCharacter(input, currentIndex, out peeked))
                {
                    nextCharacter = peeked;
                }
                else
                {
                    nextCharacter = null;
                }

                currentColumn++;
                if(currentCharacter == '\n')
                {
                    Tokenize_Whitespace(input, ref currentIndex, ref currentCharacter, ref currentLine, ref currentColumn, ref currentToken, tokens);
                    currentLine++;
                    currentColumn = 0;
                }
                else if (IsEscapeSequence(currentCharacter, nextCharacter))
                {
                    Tokenize_EscapeCharacter(input, ref currentIndex, ref currentCharacter, ref currentLine, ref currentColumn, ref currentToken, tokens);
                }
                else if(insideStringLiteral)
                {
                    Tokenize_Plain(input, ref currentIndex, ref currentCharacter, ref currentLine, ref currentColumn, ref currentToken, tokens);
                }
                else if (Delimiters.Contains("" + currentCharacter))
                {
                    Tokenize_DelimiterCharacter(input, ref currentIndex, ref currentCharacter, ref currentLine, ref currentColumn, ref currentToken, tokens);
                }
                else if (char.IsWhiteSpace(currentCharacter))
                {
                    Tokenize_Whitespace(input, ref currentIndex, ref currentCharacter, ref currentLine, ref currentColumn, ref currentToken, tokens);
                }
                else
                {
                    Tokenize_Plain(input, ref currentIndex, ref currentCharacter, ref currentLine, ref currentColumn, ref currentToken, tokens);
                }
            }

            FinalizeTokenIfNotNull(ref currentToken, tokens);

            return tokens;
        }

        private bool IsEscapeSequence(char current, char? next)
        {
            if (current != EscapeSequenceIndicator)
            {
                return false;
            }
            else if (next.HasValue == false)
            {
                return false;
            }
            else if(DoubleQuoteBehavior == PowerArgs.DoubleQuoteBehavior.IncludeQuotedTokensAsStringLiterals && next.Value == '"')
            {
                return true;
            }
            else if (Delimiters.Contains(next.Value + ""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Tokenize_EscapeCharacter(string input, ref int currentIndex, ref char currentCharacter, ref int line, ref int col, ref Token currentToken, List<T> tokens)
        {
            char nextCharacter;
            if (TryReadCharacter(input, ref currentIndex, out nextCharacter) == false)
            {
                throw new TokenizerException("Expected character after escape indicator at end of string");
            }
            else
            {
                AppendToTokenSafe(ref currentToken, nextCharacter, currentIndex, line, col);
                currentToken.SetLastCharacterAsExplicitNonDelimiter();
            }
        }

        private void Tokenize_Plain(string input, ref int currentIndex, ref char currentCharacter, ref int line, ref int col, ref Token currentToken, List<T> tokens)
        {
            if (currentCharacter == '"' && DoubleQuoteBehavior != PowerArgs.DoubleQuoteBehavior.NoSpecialHandling)
            {
                if(DoubleQuoteBehavior == PowerArgs.DoubleQuoteBehavior.IncludeQuotedTokensAsStringLiterals)
                {
                    if (insideStringLiteral == false)
                    {
                        FinalizeTokenIfNotNull(ref currentToken, tokens);
                        AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex, line, col);
                        insideStringLiteral = true;
                    }
                    else
                    {
                        AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex, line, col);
                        FinalizeTokenIfNotNull(ref currentToken, tokens);
                        insideStringLiteral = false;
                    }
                }
                else
                {
                    throw new TokenizerException("Unknown double quote option: " + DoubleQuoteBehavior);
                }
            }
            else
            {
                AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex, line, col);

                if (insideStringLiteral == false)
                {
                    var t = currentToken;
                    var delimiterMatch = (from d in Delimiters where t.EndsWithDelimiter(d) select d).OrderByDescending(d => d.Length);

                    if (delimiterMatch.Count() == 0)
                    {
                        // do nothing
                    }
                    else
                    {
                        if (Delimiters.Contains(currentToken.Value))
                        {
                            FinalizeTokenIfNotNull(ref currentToken, tokens);
                        }
                        else
                        {
                            var delimiter = delimiterMatch.First();
                            currentToken.Value = currentToken.Value.Substring(0, currentToken.Value.Length - delimiter.Length);
                            var prevToken = currentToken;
                            FinalizeTokenIfNotNull(ref currentToken, tokens);
                            currentToken = CreateTokenForTokenizer(delimiter, prevToken.StartIndex + prevToken.Value.Length, prevToken.Line, prevToken.Column + prevToken.Value.Length);
                            FinalizeTokenIfNotNull(ref currentToken, tokens);
                        }
                    }
                }
            }
        }

        private void Tokenize_DelimiterCharacter(string input, ref int currentIndex, ref char currentCharacter, ref int line, ref int col, ref Token currentToken, List<T> tokens)
        {
            FinalizeTokenIfNotNull(ref currentToken, tokens);
            currentToken = CreateTokenForTokenizer(currentCharacter,currentIndex, line, col);
            FinalizeTokenIfNotNull(ref currentToken, tokens);
        }

        private void Tokenize_Whitespace(string input, ref int currentIndex, ref char currentCharacter, ref int line, ref int col, ref Token currentToken, List<T> tokens)
        {
            if (WhitespaceBehavior == WhitespaceBehavior.DelimitAndExclude)
            {
                FinalizeTokenIfNotNull(ref currentToken, tokens);
            }
            else if (WhitespaceBehavior == WhitespaceBehavior.DelimitAndInclude)
            {
                FinalizeTokenIfNotNull(ref currentToken, tokens);
                currentToken = CreateTokenForTokenizer(currentCharacter, currentIndex, line, col);
                FinalizeTokenIfNotNull(ref currentToken, tokens);   
            }
            else if (WhitespaceBehavior == WhitespaceBehavior.Include)
            {
                AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex, line, col);
            }
            else
            {
                throw new TokenizerException("Unknown whitespace behavior: " + WhitespaceBehavior);
            }
        }

        private void FinalizeTokenIfNotNull(ref Token currentToken, List<T> tokens)
        {
            if (currentToken != null)
            {
                tokens.Add(TokenFactory(currentToken, tokens));
            }
         
            currentToken = null;
        }

        private void AppendToTokenSafe(ref Token currentToken, char toAppend, int startIndex, int line, int col)
        {
            if (currentToken == null)
            {
                currentToken = CreateTokenForTokenizer(toAppend, startIndex, line, col);
                currentToken.SourceFileLocation = this.SourceFileLocation;
            }
            else
            {
                currentToken.Value += toAppend;
            }
        }

        private static bool TryReadCharacter(string input, ref int index, out char toRead)
        {
            index++;
            if (index >= input.Length)
            {
                toRead = default(char);
                return false;
            }
            else
            {
                toRead = input[index];
                return true;
            }
        }

        private static bool TryPeekCharacter(string input, int index, out char toRead)
        {
            var peekIndex = index + 1;
            if (peekIndex >= input.Length)
            {
                toRead = default(char);
                return false;
            }
            else
            {
                toRead = input[peekIndex];
                return true;
            }
        }

        private Token CreateTokenForTokenizer(string currentCharacter, int currentIndex, int line, int col)
        {
            var ret = new Token(currentCharacter, currentIndex, line, col);
            ret.SourceFileLocation = this.SourceFileLocation;
            return ret;
        }

        private Token CreateTokenForTokenizer(char currentCharacter,int currentIndex, int line, int col)
        {
            return CreateTokenForTokenizer(currentCharacter + "", currentIndex, line, col);
        }
    }
}
