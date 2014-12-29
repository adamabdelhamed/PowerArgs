using System.Collections.Generic;

namespace PowerArgs
{
    internal class ArgParser
    {
        internal static ParseResult Parse(CommandLineArgumentsDefinition Definition, string[] commandLineArgs)
        {
            var args = commandLineArgs;

            ParseResult result = new ParseResult();

            int argumentPosition = 0;
            for (int i = 0; i < args.Length; i++)
            {
                var token = args[i];

                if (i == 0 && Definition.Actions.Count > 0 && Definition.FindMatchingAction(token) != null)
                {
                    result.ImplicitParameters.Add(0, token);
                    argumentPosition++;
                }
                else if (token.StartsWith("/"))
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
                        if (i == args.Length - 1)
                        {
                            value = "";
                        }
                        else if (IsBool(key, Definition, result))
                        {
                            var next = args[i + 1].ToLower();

                            if (next == "true" || next == "false" || next == "0" || next == "1")
                            {
                                i++;
                                value = next;
                            }
                            else
                            {
                                value = "true";
                            }
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

        internal static bool TryParseKey(string cmdLineArg, out string key)
        {
            if(cmdLineArg.StartsWith("-") == false && cmdLineArg.StartsWith("/") == false)
            {
                key = null;
                return false;
            }
            else
            {
                key = ParseKey(cmdLineArg);
                return true;
            }
        }

        internal static string ParseKey(string cmdLineArg)
        {
            if (cmdLineArg.StartsWith("/"))
            {
                var param = ParseSlashExplicitOption(cmdLineArg);
                return param.Key;
            }
            else if (cmdLineArg.StartsWith("-"))
            {
                string key = cmdLineArg.Substring(1);
                if (key.Length == 0) throw new ArgException("Missing argument value after '-'");
                

                // Handles long form syntax --argName=argValue.
                if (key.StartsWith("-") && key.Contains("="))
                {
                    var index = key.IndexOf("=");
                    key = key.Substring(0, index);
                }

                return key;
            }
            else
            {
                throw new ArgException("Could not parse key '"+cmdLineArg+"' because it did not start with a - or a /");
            }
        }

        private static bool IsBool(string key, CommandLineArgumentsDefinition definition, ParseResult resultContext)
        {
            var match = definition.FindMatchingArgument(key, true);
            if (match == null)
            {
                var possibleActionContext = resultContext.ImplicitParameters.ContainsKey(0) ? resultContext.ImplicitParameters[0] : null;

                if (possibleActionContext == null)
                {
                    return false;
                }
                else
                {
                    var actionContext = definition.FindMatchingAction(possibleActionContext, true);
                    if (actionContext == null)
                    {
                        return false;
                    }

                    match = actionContext.FindMatchingArgument(key, true);
                    if (match == null)
                    {
                        return false;
                    }
                }

            }

            return match.ArgumentType == typeof(bool);
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
