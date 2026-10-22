namespace PowerArgs;
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
    public string Value { get; internal set; } = String.Empty;

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
    public int EndIndex => StartIndex + Value.Length;
        

    /// <summary>
    /// Gets the 1 based line number of the token in the document
    /// </summary>
    public int Line { get; private set; }

    /// <summary>
    /// Gets the 1 based index of the token within it's line
    /// </summary>
    public int Column { get; private set; }


    /// <summary>
    /// Gets a string that represents the position of this token in the source document. 
    /// </summary>
    public string Position => string.Format("Line '{0}', column '{1}', source '{2}'", Line, Column, SourceFileLocation);

    /// <summary>
    /// Creates a token given an initial value, a start index, a line number, and a column number
    /// </summary>
    /// <param name="initialValue">The initial value of a token.  You can append to the token later</param>
    /// <param name="startIndex">the zero based start index of this token in the document</param>
    /// <param name="line">the 1 based line number of the token in the document</param>
    /// <param name="col">the 1 based index of the token within it's line</param>
    public Token(int startIndex, int line, int col)
    {
        if (startIndex < 0)
        {
            throw new ArgumentException("token startIndex cannot be 0");
        }

        StartIndex = startIndex;
        Line = line;
        Column = col;
    }

    /// <summary>
    /// Gets a string representation of the token, along with position info
    /// </summary>
    /// <returns>a string representation of the token, along with position info</returns>
    public override string ToString() => "---> '" + Value + "'<--- - " + Position;
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
    public char? EscapeSequenceIndicator { get; set; }

    /// <summary>
    /// A string that describes the source location for the given document
    /// </summary>
    public string SourceFileLocation { get; set; }

    private bool insideStringLiteral = false;

    public char[] TokenBuffer { get; set; } = (char[])DefaultTokenBuffer.Clone();

    public static char[] DefaultTokenBuffer = new char[1024];

    /// <summary>
    /// Creates a new tokenizer
    /// </summary>
    public Tokenizer()
    {
        this.Delimiters = new List<string>();
        this.SourceFileLocation = "Not source location specified";
        this.EscapeSequenceIndicator = '\\';
    }

    private int currentTokenIndex;
    /// <summary>
    /// Tokenizes the given string into a list of tokens
    /// </summary>
    /// <param name="input">The string to tokenize</param>
    /// <returns>The list of tokens</returns>
    public List<T> Tokenize(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        currentTokenIndex = 0;
        List<T> tokens = new List<T>();
        T currentToken = null;

        insideStringLiteral = false;

        var singleCharDelimiters = Delimiters.Where(d => d.Length == 1).Select(d => d[0]).OrderBy(d => d).ToList();
        // Whitespace-containing delimiters are only eligible when whitespace is included in tokens.
        var multiCharacterDelimiters = Delimiters
            .Where(d => d.Length > 1 && (WhitespaceBehavior == PowerArgs.WhitespaceBehavior.Include || d.Any(char.IsWhiteSpace) == false))
            .OrderByDescending(d => d.Length)
            .ToList();

        char currentCharacter;
        char? nextCharacter;
        int currentIndex = -1;
        int currentLine = 1;
        int currentColumn = 0;
        while (TryReadCharacter(input, ref currentIndex, out currentCharacter))
        {
            char peeked;
            if (TryPeekCharacter(input, currentIndex, out peeked))
            {
                nextCharacter = peeked;
            }
            else
            {
                nextCharacter = null;
            }

            currentColumn++;
            var isEscapeSequence = currentCharacter != '\n' && IsEscapeSequence(currentCharacter, nextCharacter);
            // Match multi-character delimiters before a shorter prefix can be emitted as a token.
            var multiCharacterDelimiter =
                isEscapeSequence ||
                insideStringLiteral ||
                (currentCharacter == '"' && DoubleQuoteBehavior != PowerArgs.DoubleQuoteBehavior.NoSpecialHandling)
                    ? null
                    : GetMultiCharacterDelimiterAt(input, currentIndex, multiCharacterDelimiters);

            if (multiCharacterDelimiter != null)
            {
                Tokenize_MultiCharacterDelimiter(multiCharacterDelimiter, ref currentIndex, ref currentLine, ref currentColumn, ref currentToken, tokens);
            }
            else if (currentCharacter == '\n')
            {
                Tokenize_Whitespace(input, ref currentIndex, ref currentCharacter, currentLine, ref currentColumn, ref currentToken, tokens);
                currentLine++;
                currentColumn = 0;
            }
            else if (isEscapeSequence)
            {
                Tokenize_EscapeCharacter(input, ref currentIndex, ref currentCharacter, currentLine, ref currentColumn, ref currentToken, tokens);
            }
            else if (insideStringLiteral)
            {
                Tokenize_Plain(input, ref currentIndex, ref currentCharacter, currentLine, ref currentColumn, ref currentToken, tokens);
            }
            else if (singleCharDelimiters.BinarySearch(currentCharacter) >= 0)
            {
                Tokenize_DelimiterCharacter(input, ref currentIndex, ref currentCharacter, currentLine, ref currentColumn, ref currentToken, tokens);
            }
            else if (char.IsWhiteSpace(currentCharacter))
            {
                Tokenize_Whitespace(input, ref currentIndex, ref currentCharacter, currentLine, ref currentColumn, ref currentToken, tokens);
            }
            else
            {
                Tokenize_Plain(input, ref currentIndex, ref currentCharacter, currentLine, ref currentColumn, ref currentToken, tokens);
            }
        }

        FinalizeTokenIfNotNull(ref currentToken, tokens);

        return tokens;
    }

    private bool IsEscapeSequence(char current, char? next)
    {
        if (EscapeSequenceIndicator.HasValue == false || current != EscapeSequenceIndicator.Value)
        {
            return false;
        }
        else if (next.HasValue == false)
        {
            return false;
        }
        else if (DoubleQuoteBehavior == PowerArgs.DoubleQuoteBehavior.IncludeQuotedTokensAsStringLiterals && next.Value == '"')
        {
            return true;
        }
        else if (Delimiters.Count > 0 && Delimiters.Contains(next.Value + ""))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Tokenize_EscapeCharacter(string input, ref int currentIndex, ref char currentCharacter, int line, ref int col, ref T currentToken, List<T> tokens)
    {
        char nextCharacter;
        if (TryReadCharacter(input, ref currentIndex, out nextCharacter) == false)
        {
            throw new TokenizerException("Expected character after escape indicator at end of string");
        }
        else
        {
            AppendToTokenSafe(ref currentToken, nextCharacter, currentIndex, line, col);
        }
    }

    private void Tokenize_Plain(string input, ref int currentIndex, ref char currentCharacter, int line, ref int col, ref T currentToken, List<T> tokens)
    {
        if (currentCharacter == '"' && DoubleQuoteBehavior != PowerArgs.DoubleQuoteBehavior.NoSpecialHandling)
        {
            if (DoubleQuoteBehavior == PowerArgs.DoubleQuoteBehavior.IncludeQuotedTokensAsStringLiterals)
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
        }
    }

    private string? GetMultiCharacterDelimiterAt(string input, int index, List<string> delimiters)
    {
        foreach (var delimiter in delimiters)
        {
            if (delimiter[0] == input[index] && delimiter.Length <= input.Length - index &&
                string.CompareOrdinal(input, index, delimiter, 0, delimiter.Length) == 0 &&
                CanConsumeDelimiter(input, index, delimiter.Length))
            {
                return delimiter;
            }
        }

        return null;
    }

    private bool CanConsumeDelimiter(string input, int index, int length)
    {
        // Lookahead must not swallow characters that the ordinary scanner would handle specially.
        // Inspect the next source character even when an escape indicator ends the delimiter.
        for (var i = index; i < index + length; i++)
        {
            if (input[i] == '"' && DoubleQuoteBehavior != PowerArgs.DoubleQuoteBehavior.NoSpecialHandling) return false;
            char? next = i + 1 < input.Length ? input[i + 1] : null;
            if (input[i] != '\n' && IsEscapeSequence(input[i], next)) return false;
        }

        return true;
    }

    private void Tokenize_MultiCharacterDelimiter(string delimiter, ref int currentIndex, ref int line, ref int col, ref T currentToken, List<T> tokens)
    {
        FinalizeTokenIfNotNull(ref currentToken, tokens);
        currentToken = CreateTokenForTokenizer(currentIndex, line, col);
        currentToken.SourceFileLocation = SourceFileLocation;

        for (var i = 0; i < delimiter.Length; i++)
        {
            if (i > 0)
            {
                currentIndex++;
                col++;
            }

            AppendToTokenSafe(ref currentToken, delimiter[i], currentIndex, line, col);
            if (delimiter[i] == '\n')
            {
                line++;
                col = 0;
            }
        }

        FinalizeTokenIfNotNull(ref currentToken, tokens);
    }

    private void Tokenize_DelimiterCharacter(string input, ref int currentIndex, ref char currentCharacter, int line, ref int col, ref T currentToken, List<T> tokens)
    {
        FinalizeTokenIfNotNull(ref currentToken, tokens);
        currentToken = CreateTokenForTokenizer(currentIndex, line, col);
        AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex, line, col);
        FinalizeTokenIfNotNull(ref currentToken, tokens);
    }

    private void Tokenize_Whitespace(string input, ref int currentIndex, ref char currentCharacter, int line, ref int col, ref T currentToken, List<T> tokens)
    {
        if (WhitespaceBehavior == WhitespaceBehavior.DelimitAndExclude)
        {
            FinalizeTokenIfNotNull(ref currentToken, tokens);
        }
        else if (WhitespaceBehavior == WhitespaceBehavior.DelimitAndInclude)
        {
            FinalizeTokenIfNotNull(ref currentToken, tokens);
            currentToken = CreateTokenForTokenizer(currentIndex, line, col);
            AppendToTokenSafe(ref currentToken, currentCharacter, currentIndex, line, col);
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

    private void FinalizeTokenIfNotNull(ref T currentToken, List<T> tokens)
    {
        if (currentToken != null)
        {
            currentToken.Value = new string(TokenBuffer, 0, currentTokenIndex);
            tokens.Add(currentToken);
        }

        currentToken = null;
        currentTokenIndex = 0;
    }


    private void AppendToTokenSafe(ref T currentToken, char toAppend, int startIndex, int line, int col)
    {
        if (currentTokenIndex == TokenBuffer.Length)
        {
            var expandedBuffer = TokenBuffer;
            Array.Resize(ref expandedBuffer, Math.Max(16, expandedBuffer.Length * 2));
            TokenBuffer = expandedBuffer;
        }

        if (currentToken == null)
        {
            currentToken = CreateTokenForTokenizer(startIndex, line, col);
            TokenBuffer[currentTokenIndex++] = toAppend;
            currentToken.SourceFileLocation = this.SourceFileLocation;
        }
        else
        {
            TokenBuffer[currentTokenIndex++] = toAppend;
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

    protected virtual T TokenFactory(int currentIndex, int line, int col)
    {
        T ret;
        if (typeof(T) == typeof(Token))
        {
            ret = new Token(currentIndex, line, col) as T;
        }
        else
        {
            ret = (T)Activator.CreateInstance(typeof(T), currentIndex, line, col);
        }
        return ret;
    }

    private T CreateTokenForTokenizer(int currentIndex, int line, int col)
    {
        return TokenFactory(currentIndex, line, col);
    }
}
