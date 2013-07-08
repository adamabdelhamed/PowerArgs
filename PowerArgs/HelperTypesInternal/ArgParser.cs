using System.Collections.Generic;

namespace PowerArgs
{
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
                    if (result.ExplicitParameters.ContainsKey(param.Key)) throw new DuplicateArgException("Argument specified more than once: " + param.Key);
                    result.ExplicitParameters.Add(param.Key, param.Value);
                    argumentPosition = -1;
                }
                else if (token.StartsWith("-"))
                {
                    string key = token.Substring(1);

                    if (key.Length == 0) throw new ArgException("Missing argument value after '-'");

                    string value;

                    // Handles long form syntax --argName=argValue.
                    if (key.StartsWith("-") && key.Contains("="))
                    {
                        var index = key.IndexOf("=");
                        value = key.Substring(index + 1);
                        key = key.Substring(0, index);
                    }
                    else
                    {
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
                    }

                    if (result.ExplicitParameters.ContainsKey(key))
                    {
                        throw new DuplicateArgException("Argument specified more than once: " + key);
                    }

                    result.ExplicitParameters.Add(key, value);
                    argumentPosition = -1;
                }
                else
                {
                    if (argumentPosition < 0) throw new UnexpectedArgException("Unexpected argument: " + token);
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
