using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class TokenReader<T> where T : Token
    {
        private List<T> tokens;
        private int currentIndex;

        public T CurrentToken
        {
            get
            {
                if(currentIndex < 0)
                {
                    throw new IndexOutOfRangeException("You have not advanced the reader for the first time");
                }

                return tokens[currentIndex];
            }
        }

        public TokenReader(List<T> tokens)
        {
            if (tokens == null) throw new ArgumentNullException("tokens cannot be null");
            this.tokens = tokens;
            currentIndex = -1;
        }

        public T Advance(bool skipWhitespace = false)
        {
            T ret;
            if (TryAdvance(out ret, skipWhitespace) == false)
            {
                throw new DocumentRenderedException("Unexpected end of file");
            }
            return ret;
        }

        public T Peek(bool skipWhitespace = false)
        {
            T ret;
            int lastPeekIndex;
            if (TryPeek(out ret, out lastPeekIndex,skipWhitespace: skipWhitespace) == false)
            {
                throw new DocumentRenderedException("Unexpected end of file");
            }
            return ret;
        }

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

        public bool CanAdvance(bool skipWhitespace = false)
        {
            T tokenDummy;
            int lastPeekIndexDummy;
            return TryPeek(out tokenDummy, out lastPeekIndexDummy, skipWhitespace: skipWhitespace);
        }

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
    }
}
