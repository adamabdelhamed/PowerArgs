using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs
{
    [Obsolete("The ArgStyle attribute is obsolete.  Both styles are now supported automatically")]
    public enum ArgStyle
    {
        PowerShell,  // named args are specified in the format "exeName -param1Name param1Value" 
        SlashColon   // named args are specified in the format "exeName -/param1Name:param1Value"
    }

    public class ParseResult
    {
        public Dictionary<string, string> ExplicitParameters { get; private set; }
        public Dictionary<int, string> ImplicitParameters { get; private set; }

        public string ActionParameter { get; set; }

        public ParseResult()
        {
            ExplicitParameters = new Dictionary<string, string>();
            ImplicitParameters = new Dictionary<int, string>();
        }
    }

    internal class ArgParser
    {
        internal static ParseResult Parse(string[] args)
        {
            ParseResult result = new ParseResult();

            int argumentPosition = 0;
            for (int i = 0; i < args.Length; i++)
            {
                var token = args[i];

                if (token.StartsWith("/"))
                {
                    var param = ParseSlashExplicitOption(token);
                    if (result.ExplicitParameters.ContainsKey(param.Key)) throw new ArgException("Argument " + param.Key + " cannot be specified twice");
                    result.ExplicitParameters.Add(param.Key, param.Value);
                    argumentPosition = -1;
                }
                else if (token.StartsWith("-"))
                {
                    string key = token.Substring(1);

                    if (key.Length == 0) throw new ArgException("Missing argument value after '-'");

                    string value;

                    if (i == args.Length - 1 ||
                        (args[i + 1].StartsWith("-") && args[i + 1].Length > 1 && !char.IsDigit(args[i + 1][1])) ||
                        args[i + 1].StartsWith("/"))
                    {
                        value = "";
                    }
                    else
                    {
                        i++;
                        value = args[i];
                    }

                    result.ExplicitParameters.Add(key, value);
                    argumentPosition = -1;
                }
                else
                {
                    if (argumentPosition < 0) throw new ArgException("Unexpected argument: " + token);
                    result.ImplicitParameters.Add(argumentPosition, token);
                    argumentPosition++;
                }
            }

            return result;
        }

        private static KeyValuePair<string, string> ParseSlashExplicitOption(string a)
        {
            var key = a.Contains(":") ? a.Substring(1, a.IndexOf(":") - 1).Trim() : a.Substring(1, a.Length - 1);
            var value = a.Contains(":") ? a.Substring(a.IndexOf(":") + 1).Trim() : "";

            if (key.Length == 0) throw new ArgException("Missing argument value after '/'");

            return new KeyValuePair<string, string>(key, value);
        }
    }
}
