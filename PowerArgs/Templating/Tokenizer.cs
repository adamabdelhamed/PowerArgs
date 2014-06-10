﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class TokenizerException : Exception
    {
        public TokenizerException(string message) : base(message) { }
    }

    public class Token
    {
        public string Value { get; internal set; }
        public int StartIndex { get; private set; }
        public int EndIndex
        {
            get
            {
                return StartIndex + Value.Length;
            }
        }

        public int Line { get; private set; }
        public int Column { get; set; }

        public List<int> ExplicitNonDelimiterCharacters { get; private set; }

        public string Position
        {
            get
            {
                return string.Format("Line '{0}', column '{1}'", Line, Column);
            }

        }
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

        public Token(char firstCharacter, int startIndex, int line, int col) : this("" + firstCharacter, startIndex, line, col) { }

        public void Append(string s)
        {
            Value += s;
        }

        public void Append(char c)
        {
            Append("" + c);
        }

        public void SetLastCharacterAsExplicitNonDelimiter()
        {
            if(this.Value.Length == 0)
            {
                throw new InvalidOperationException("This token has no value");
            }
            ExplicitNonDelimiterCharacters.Add(this.Value.Length - 1);
        }

        public bool EndsWithDelimiter(string delimiter)
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
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            return "---> '" + Value + "'<--- - " + Position;
        }

        public T As<T>()
        {
            var ret = (T)Activator.CreateInstance(typeof(T), this.Value, this.StartIndex, this.Line, this.Column);
            return ret;
        }
    }

    public enum WhitespaceBehavior
    {
        DelimitAndInclude,
        DelimitAndExclude,
        Include,
    }

    public class Tokenizer<T> where T : Token
    {
        public List<string> Delimiters { get; private set; }

        public Func<Token, List<T>, T> TokenFactory { get; set; }

        public WhitespaceBehavior WhitespaceBehavior { get; set; }

        public char EscapeSequenceIndicator { get; set; }

        public Tokenizer()
        {
            this.Delimiters = new List<string>();
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

        public List<T> Tokenize(string input)
        {
            List<T> tokens = new List<T>();
            Token currentToken = null;

            char currentCharacter;
            int currentIndex = -1;
            int currentLine = 1;
            int currentColumn = 0;
            while (TryReadCharacter(input, ref currentIndex, out currentCharacter))
            {
                currentColumn++;
                if(currentCharacter == '\n')
                {
                    Tokenize_Whitespace(input, ref currentIndex, ref currentCharacter, ref currentLine, ref currentColumn, ref currentToken, tokens);
                    currentLine++;
                    currentColumn = 0;
                }
                else if (currentCharacter == EscapeSequenceIndicator)
                {
                    Tokenize_EscapeCharacter(input, ref currentIndex, ref currentCharacter, ref currentLine, ref currentColumn, ref currentToken, tokens);
                }
                if (Delimiters.Contains("" + currentCharacter))
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
            AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex, line, col);

            var t = currentToken;
            var delimiterMatch = (from d in Delimiters where t.EndsWithDelimiter(d) select d).OrderByDescending(d => d.Length);

            if(delimiterMatch.Count() == 0)
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
                    currentToken = new Token(delimiter, prevToken.StartIndex + prevToken.Value.Length, prevToken.Line, prevToken.Column + prevToken.Value.Length);
                    FinalizeTokenIfNotNull(ref currentToken, tokens);
                }
            }
        }

        private void Tokenize_DelimiterCharacter(string input, ref int currentIndex, ref char currentCharacter, ref int line, ref int col, ref Token currentToken, List<T> tokens)
        {
            FinalizeTokenIfNotNull(ref currentToken, tokens);
            currentToken = new Token(currentCharacter, currentIndex, line, col);
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
                if (IsWhitespace(currentToken))
                {
                    currentToken.Append(currentCharacter);
                }
                else
                {
                    FinalizeTokenIfNotNull(ref currentToken, tokens);
                    currentToken = new Token(currentCharacter, currentIndex, line, col);
                    FinalizeTokenIfNotNull(ref currentToken, tokens);
                }
            }
            else if (WhitespaceBehavior == WhitespaceBehavior.Include)
            {
                AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex, line, col);
            }
            else
            {
                throw new Exception("Unknown whitespace behavior: " + WhitespaceBehavior);
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
                currentToken = new Token(toAppend, startIndex, line, col);
            }
            else
            {
                currentToken.Append(toAppend);
            }
        }

        private bool IsWhitespace(Token token)
        {
            if (token == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(token.Value))
            {
                return true;
            }
            else
            {
                return false;
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
    }
}