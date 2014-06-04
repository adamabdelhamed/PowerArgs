using System;
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
        public string Value { get; private set; }
        public int StartIndex { get; private set; }

        public int EndIndex
        {
            get
            {
                return StartIndex + Value.Length;
            }
        }

        public Token(string initialValue, int startIndex)
        {
            if (startIndex < 0)
            {
                throw new ArgumentException("token startIndex cannot be 0");
            }

            Value = initialValue;
            StartIndex = startIndex;
        }

        public Token(char firstCharacter, int startIndex) : this("" + firstCharacter, startIndex) { }

        public void Append(string s)
        {
            Value += s;
        }

        public void Append(char c)
        {
            Append("" + c);
        }

        public override string ToString()
        {
            return "'" + Value + "' - StartIndex: " + StartIndex;
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
                this.TokenFactory = (token, previousTokens) => (T)Activator.CreateInstance(typeof(T), token.Value, token.StartIndex);
            }

            this.EscapeSequenceIndicator = '\\';
        }

        public List<T> Tokenize(string input)
        {
            List<T> tokens = new List<T>();
            Token currentToken = null;

            char currentCharacter;
            int currentIndex = -1;

            while (TryReadCharacter(input, ref currentIndex, out currentCharacter))
            {
                if (currentCharacter == EscapeSequenceIndicator)
                {
                    Tokenize_EscapeCharacter(input, ref currentIndex, ref currentCharacter, ref currentToken, tokens);
                }
                if (Delimiters.Contains("" + currentCharacter))
                {
                    Tokenize_DelimiterCharacter(input, ref currentIndex, ref currentCharacter, ref currentToken, tokens);
                }
                else if (char.IsWhiteSpace(currentCharacter))
                {
                    Tokenize_Whitespace(input, ref currentIndex, ref currentCharacter, ref currentToken, tokens);
                }
                else
                {
                    Tokenize_Plain(input, ref currentIndex, ref currentCharacter, ref currentToken, tokens);
                }
            }

            FinalizeTokenIfNotNull(ref currentToken, tokens);

            return tokens;
        }

        private void Tokenize_EscapeCharacter(string input, ref int currentIndex, ref char currentCharacter, ref Token currentToken, List<T> tokens)
        {
            char nextCharacter;
            if (TryReadCharacter(input, ref currentIndex, out nextCharacter) == false)
            {
                throw new TokenizerException("Expected character after escape indicator at end of string");
            }
            else
            {
                AppendToTokenSafe(ref currentToken, nextCharacter, currentIndex);
            }
        }

        private void Tokenize_Plain(string input, ref int currentIndex, ref char currentCharacter, ref Token currentToken, List<T> tokens)
        {
            AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex);

            if (Delimiters.Contains(currentToken.Value))
            {
                FinalizeTokenIfNotNull(ref currentToken, tokens);
            }
        }

        private void Tokenize_DelimiterCharacter(string input, ref int currentIndex, ref char currentCharacter, ref Token currentToken, List<T> tokens)
        {
            FinalizeTokenIfNotNull(ref currentToken, tokens);
            currentToken = new Token(currentCharacter, currentIndex);
            FinalizeTokenIfNotNull(ref currentToken, tokens);
        }

        private void Tokenize_Whitespace(string input, ref int currentIndex, ref char currentCharacter, ref Token currentToken, List<T> tokens)
        {
            if (WhitespaceBehavior == WhitespaceBehavior.DelimitAndExclude)
            {
                FinalizeTokenIfNotNull(ref currentToken, tokens);
            }
            else if (WhitespaceBehavior == WhitespaceBehavior.DelimitAndInclude)
            {
                if (IsWhitespace(currentToken.Value))
                {
                    currentToken.Append(currentCharacter);
                }
                else
                {
                    FinalizeTokenIfNotNull(ref currentToken, tokens);
                    currentToken = new Token(currentCharacter, currentIndex);
                }
            }
            else if (WhitespaceBehavior == WhitespaceBehavior.Include)
            {
                AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex);
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

        private void AppendToTokenSafe(ref Token currentToken, char toAppend, int startIndex)
        {
            if (currentToken == null)
            {
                currentToken = new Token(toAppend, startIndex);
            }
            else
            {
                currentToken.Append(toAppend);
            }
        }

        private bool IsWhitespace(string s)
        {
            if (s == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(s))
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
