using System;
using System.Collections.Generic;
using System.Linq;

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

        public static string GetTokenTypeValue(DocumentTokenType type)
        {
            string ret;
            if(TryGetTokenTypeValue(type, out ret) == false)
            {
                throw new ArgumentException("The type '"+type+"' does not have a constant string value");
            }
            return ret;
        }

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

        public static List<DocumentToken> Tokenize(string text, string sourceLocation)
        {
            Tokenizer<DocumentToken> tokenizer = new Tokenizer<DocumentToken>();
            tokenizer.SourceFileLocation = sourceLocation;
            tokenizer.Delimiters.AddRange(DocumentToken.Delimiters);
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndInclude;
            tokenizer.TokenFactory = DocumentToken.TokenFactoryImpl;
            List<DocumentToken> tokens = tokenizer.Tokenize(text);
            return tokens;
        }

        public static DocumentToken TokenFactoryImpl(Token token, List<DocumentToken> previous)
        {
            var utToken = token.As<DocumentToken>();

            DocumentTokenType constantValueType;
            if(TryParseDocumentTokenType(token.Value, out constantValueType))
            {
                utToken.TokenType = constantValueType;
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

        public static List<DocumentToken> RemoveLinesThatOnlyContainReplacements(List<DocumentToken> tokens)
        {
            var currentLine = 1;
            int numContentTokensOnCurrentLine = 0;
            int numReplacementTokensOnCurrentLine = 0;

            List<DocumentToken> filtered = new List<DocumentToken>();
            foreach (var token in tokens)
            {
                if (token.Line != currentLine)
                {
                    currentLine = token.Line;

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

            return filtered;
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
