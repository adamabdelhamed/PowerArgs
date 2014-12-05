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
        private List<T> tokens;
        private int currentIndex;

        /// <summary>
        /// Creates a token reader given a list of tokens
        /// </summary>
        /// <param name="tokens">The list of tokens to read through</param>
        public TokenReader(IEnumerable<T> tokens)
        {
            if (tokens == null) throw new ArgumentNullException("tokens cannot be null");
            this.tokens = tokens.ToList();
            currentIndex = -1;
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
        /// Gets the next token in the list without actually advancing the reader
        /// </summary>
        /// <param name="skipWhitespace">If true, the reader will skip past whitespace tokens when reading</param>
        /// <returns>The next token in the list</returns>
        public T Peek(bool skipWhitespace = false)
        {
            T ret;
            int lastPeekIndex;
            if (TryPeek(out ret, out lastPeekIndex,skipWhitespace: skipWhitespace) == false)
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
            if(peekResult)
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
            if(lookAhead <= 0)
            {
                throw new ArgumentOutOfRangeException("lookAhead must be >= 1");
            }

            ret = null;
            lastPeekIndex = -1;

            for (int i = 0; i < lookAhead; i++ )
            {
                if (TryPeekOnce(out ret, out lastPeekIndex, skipWhitespace) == false)
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
                if (peekIndex >= tokens.Count)
                {
                    ret = null;
                    lastPeekIndex = -1;
                    return false;
                }
            }
            while (skipWhitespace && string.IsNullOrWhiteSpace(tokens[peekIndex].Value));

            ret = tokens[peekIndex];
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
        public string ToString(bool skipWhitespace= false)
        {
            var ret = "";
            foreach(var token in tokens)
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
