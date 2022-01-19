using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// A class that makes it easy to read through a list of tokens
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TokenReader<T> where T : Token
    {
        /// <summary>
        /// Gets the tokens that were passed into the reader as a list
        /// </summary>
        public List<T> Tokens { get; private set; }
        private int currentIndex;

        /// <summary>
        /// Creates a token reader given a list of tokens
        /// </summary>
        /// <param name="tokens">The list of tokens to read through</param>
        public TokenReader(IEnumerable<T> tokens)
        {
            if (tokens == null) throw new ArgumentNullException("tokens cannot be null");
            this.Tokens = tokens.ToList();
            currentIndex = -1;
        }

        public int Position
        {
            get
            {
                return currentIndex;
            }
            set
            {
                currentIndex = value;
            }
        }

        public T Current
        {
            get
            {
                return Tokens[Position];
            }
        }

        public void RewindOne()
        {
            if (currentIndex == -1)
            {
                throw new IndexOutOfRangeException();
            }
            currentIndex--;
        }

        /// <summary>
        /// Advances the reader to the next token
        /// </summary>
        /// <param name="skipWhitespace">If true, the reader will skip past whitespace tokens when reading</param>
        /// <returns>the next token in the list</returns>
        public T Advance(bool skipWhitespace = false)
        {
            T ret;
            if (TryAdvance(out ret, skipWhitespace) == false)
            {
                throw new IndexOutOfRangeException("Unexpected end of file");
            }
            return ret;
        }

        /// <summary>
        /// Advances the reader and asserts that there is a value and that it matches the expected value.  If the assertion
        /// fails then a FormatException is thrown.
        /// </summary>
        /// <param name="skipWhiteSpace">determines if whitespace tokens are skipped</param>
        /// <param name="expectedValue">the token value to expect</param>
        /// <param name="comparison">how to compare the strings</param>
        /// <returns>the token which matches the expected value</returns>
        public T Expect(string expectedValue, bool skipWhiteSpace = false, StringComparison comparison = StringComparison.Ordinal)
        {
            T ret;
            if (TryAdvance(out ret, skipWhiteSpace) == false)
            {
                throw new FormatException($"Expected '{expectedValue}', got end of string");
            }

            if (ret.Value.Equals(expectedValue, comparison) == false)
            {
                throw new FormatException($"Expected '{expectedValue}', got '{ret.Value}' @ {ret.Position}");
            }

            return ret;
        }

        /// <summary>
        /// Gets the next token in the list without actually advancing the reader
        /// </summary>
        /// <param name="skipWhitespace">If true, the reader will skip past whitespace tokens when reading</param>
        /// <returns>The next token in the list</returns>
        public T Peek(bool skipWhitespace = false)
        {
            T ret;
            int lastPeekIndex;
            if (TryPeek(out ret, out lastPeekIndex, skipWhitespace: skipWhitespace) == false)
            {
                throw new IndexOutOfRangeException("Unexpected end of file");
            }
            return ret;
        }

        /// <summary>
        /// Advances the reader to the next token if one exists.
        /// </summary>
        /// <param name="ret">The out variable to store the token if it was found</param>
        /// <param name="skipWhitespace">If true, the reader will skip past whitespace tokens when reading</param>
        /// <returns>True if the reader advanced, false otherwise</returns>
        public bool TryAdvance(out T ret, bool skipWhitespace = false)
        {
            int peekIndex;
            bool peekResult = TryPeekOnce(out ret, out peekIndex, skipWhitespace: skipWhitespace);
            if (peekResult)
            {
                currentIndex = peekIndex;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if the reader can advance
        /// </summary>
        /// <param name="skipWhitespace">If true, the reader will skip past whitespace tokens when reading</param>
        /// <returns>True if there is another token to read, false otherwise</returns>
        public bool CanAdvance(bool skipWhitespace = false)
        {
            T tokenDummy;
            int lastPeekIndexDummy;
            return TryPeek(out tokenDummy, out lastPeekIndexDummy, skipWhitespace: skipWhitespace);
        }

        /// <summary>
        /// Reads the next token without advancing if one is available.
        /// </summary>
        /// <param name="ret">The out variable to store the token if it was found</param>
        /// <param name="lastPeekIndex">The out variable to store the index of the peeked token in the token list</param>
        /// <param name="lookAhead">How far to peek ahead, by default 1</param>
        /// <param name="skipWhitespace">If true, the reader will skip past whitespace tokens when reading</param>
        /// <returns>True if the reader peeked at a value, false otherwise</returns>
        public bool TryPeek(out T ret, out int lastPeekIndex, int lookAhead = 1, bool skipWhitespace = false)
        {
            if (lookAhead <= 0)
            {
                throw new ArgumentOutOfRangeException("lookAhead must be >= 1");
            }

            ret = null;
            lastPeekIndex = -1;

            for (int i = 0; i < lookAhead; i++)
            {
                if (TryPeekOnce(out ret, out lastPeekIndex, skipWhitespace) == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to look backward for a previous token
        /// </summary>
        /// <param name="ret">the previous token, if found</param>
        /// <param name="lastPeekIndex">an out variable that will be set to the index of the previous token if found</param>
        /// <param name="lookBehind">the number of positions to move backward</param>
        /// <param name="skipWhitespace">don't count whitespace</param>
        /// <returns>true if a previous token was found, false otherwise</returns>
        public bool TryPeekBehind(out T ret, out int lastPeekIndex, int lookBehind = 1, bool skipWhitespace = false)
        {
            if (lookBehind <= 0)
            {
                throw new ArgumentOutOfRangeException("lookBehind must be >= 1");
            }

            ret = null;
            lastPeekIndex = -1;

            for (int i = 0; i < lookBehind; i++)
            {
                if (TryPeekBehindOnce(out ret, out lastPeekIndex, skipWhitespace) == false)
                {
                    return false;
                }
            }

            return true;
        }
 

        private bool TryPeekOnce(out T ret, out int lastPeekIndex, bool skipWhitespace = false)
        {
            int peekIndex = currentIndex;
            do
            {
                peekIndex++;
                if (peekIndex >= Tokens.Count)
                {
                    ret = null;
                    lastPeekIndex = -1;
                    return false;
                }
            }
            while (skipWhitespace && string.IsNullOrWhiteSpace(Tokens[peekIndex].Value));

            ret = Tokens[peekIndex];
            lastPeekIndex = peekIndex;
            return true;
        }

        private bool TryPeekBehindOnce(out T ret, out int lastPeekIndex, bool skipWhitespace = false)
        {
            int peekIndex = currentIndex;
            do
            {
                peekIndex--;
                if (peekIndex < 0)
                {
                    ret = null;
                    lastPeekIndex = -1;
                    return false;
                }
            }
            while (skipWhitespace && string.IsNullOrWhiteSpace(Tokens[peekIndex].Value));

            ret = Tokens[peekIndex];
            lastPeekIndex = peekIndex;
            return true;
        }

        /// <summary>
        /// Gets all the tokens in the list concatenated into a single string, including whitespace
        /// </summary>
        /// <returns>all the tokens in the list concatenated into a single string, including whitespace</returns>
        public override string ToString()
        {
            return ToString(skipWhitespace: false);
        }

        /// <summary>
        /// Gets all the tokens in the list concatenated into a single string, optionally excluding whitespace
        /// </summary>
        /// <param name="skipWhitespace">If true, whitespace tokens will not be included in the output.  Tokens that have
        /// whitespace and non whitespace characters will always be included</param>
        /// <returns>all the tokens in the list concatenated into a single string, with whitespace tokens optionally excluded</returns>
        public string ToString(bool skipWhitespace = false)
        {
            var ret = "";
            foreach (var token in Tokens)
            {
                if (skipWhitespace == true && string.IsNullOrWhiteSpace(token.Value))
                {
                    // skip
                }
                else
                {
                    ret += token.Value;
                }
            }
            return ret;
        }
    }
}
