using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public enum DocumentTokenType
    {
        ReplacementKey,
        ReplacementParameter,
        PlainText,
        BeginReplacementSegment,
        EndReplacementSegment,
        BeginTerminateReplacementSegment,
        QuickTerminateReplacementSegment,
    }

    public class DocumentToken : Token
    {
        public DocumentTokenType TokenType { get; set; }

        public DocumentToken(string initialValue, int startIndex, int line, int col) : base(initialValue, startIndex, line, col) { }

        public static List<string> Delimiters
        {
            get
            {
                return new List<string>()
                {
                    "!{{",
                    "!}}",
                    "{{",
                    "}}",
                };
            }
        }

        public static List<DocumentToken> Tokenize(string text)
        {
            Tokenizer<DocumentToken> tokenizer = new Tokenizer<DocumentToken>();
            tokenizer.Delimiters.AddRange(DocumentToken.Delimiters);
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndInclude;
            tokenizer.TokenFactory = DocumentToken.TokenFactoryImpl;
            List<DocumentToken> tokens = tokenizer.Tokenize(text);
            return tokens;
        }

        public static DocumentToken TokenFactoryImpl(Token token, List<DocumentToken> previous)
        {
            var utToken = token.As<DocumentToken>();
            if (utToken.Value == "{{")
            {
                utToken.TokenType = DocumentTokenType.BeginReplacementSegment;
            }
            else if (utToken.Value == "}}")
            {
                utToken.TokenType = DocumentTokenType.EndReplacementSegment;
            }
            else if (utToken.Value == "!{{")
            {
                utToken.TokenType = DocumentTokenType.BeginTerminateReplacementSegment;
            }
            else if (utToken.Value == "!}}")
            {
                utToken.TokenType = DocumentTokenType.QuickTerminateReplacementSegment;
            }
            else if (previous.Count > 0 && (previous.Last().TokenType == DocumentTokenType.BeginReplacementSegment || previous.Last().TokenType == DocumentTokenType.BeginTerminateReplacementSegment))
            {
                utToken.TokenType = DocumentTokenType.ReplacementKey;
            }
            else if (previous.Count > 0 && (previous.Last().TokenType == DocumentTokenType.ReplacementKey || previous.Last().TokenType == DocumentTokenType.ReplacementParameter))
            {
                utToken.TokenType = DocumentTokenType.ReplacementParameter;
            }
            else
            {
                utToken.TokenType = DocumentTokenType.PlainText;
            }

            return utToken;
        }
    }
}
