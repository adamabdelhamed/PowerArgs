using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    internal class ArgParser
    {
        // todo - This class was originally very dumb.  It parsed the command line arguments without knowledge of the definition.
        //        However, several special syntaxes that folks were expecting would only be possible if the parser had pretty deep
        //        knowledge of the program structure.  So now this class takes in the definition and inspects it to handle these
        //        special cases.  I should finish the job and handle positional elements this way too.  This would remove the need
        //        for the 'ImplicitParameters' collection in the ParseResult.  On that other hand that would be a breaking change just
        //        for the sake of cleanup. I need to think it over.
        //
        //        Another potential item would be to refactor the parse method here.  It's a mess, but it's a working, heavily tested mess
        //        so cleaning it up will mean accepting some risk.

        internal static ParseResult Parse(CommandLineArgumentsDefinition Definition, string[] commandLineArgs)
        {
            var args = commandLineArgs;

            ParseResult result = new ParseResult();

            int argumentPosition = 0;
            for (int i = 0; i < args.Length; i++)
            {
                var token = args[i];

                // this block handles action parameters that must always be the first token
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
                else if (token == "-")
                {
                    throw new ArgException("Missing argument value after '-'");
                }
                else if (IsDashSpecifiedArgumentIdentifier(token))
                {
                    string key = token.Substring(1);
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

                    if(IsArrayOrList(key, Definition, result))
                    {
                        while((i+1) < args.Length)
                        {
                            var nextToken = args[i+1];

                            if(nextToken.StartsWith("/") || IsDashSpecifiedArgumentIdentifier(nextToken))
                            {
                                break;
                            }
                            else
                            {
                                result.AddAdditionalParameter(key, nextToken);
                                i++;
                            }

                        }
                    }

                    argumentPosition = -1;
                }
                else
                {
                    if (argumentPosition < 0) throw new UnexpectedArgException("Unexpected argument: " + token);

                    var possibleActionContext = result.ImplicitParameters.ContainsKey(0) ? result.ImplicitParameters[0] : null;
                    var potentialListArgument = Definition.FindArgumentByPosition(argumentPosition, possibleActionContext);
                    
                    if (potentialListArgument != null)
                    {
                        bool isArrayOrList = potentialListArgument.ArgumentType.IsArray || potentialListArgument.ArgumentType.GetInterfaces().Contains(typeof(IList));

                        if (isArrayOrList)
                        {
                            // this block does special handling to allow for space separated collections for positioned parameters

                            result.ExplicitParameters.Add(potentialListArgument.DefaultAlias, token);
                            argumentPosition = -1; // no more positional arguments are allowed after this
                            while ((i + 1) < args.Length)
                            {
                                var nextToken = args[i + 1];

                                if (nextToken.StartsWith("/") || IsDashSpecifiedArgumentIdentifier(nextToken))
                                {
                                    break;
                                }
                                else
                                {
                                    result.AddAdditionalParameter(potentialListArgument.DefaultAlias, nextToken);
                                    i++;
                                }

                            }
                        }
                        else
                        {
                            // not an array or list parameter so add to the implicit parameter collection
                            result.ImplicitParameters.Add(argumentPosition, token);
                            argumentPosition++;
                        }
                    }
                    else
                    {
                        // not an array or list parameter so add to the implicit parameter collection
                        result.ImplicitParameters.Add(argumentPosition, token);
                        argumentPosition++;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Since negative numbers and dash specified keys (e.g. -name & -1) both start with dashes
        /// PowerArgs used to have cases where it would get confused because lots of code was just checking to
        /// see if the token starts with a dash. This helper will only return true if the first character after 
        /// an initial dash is a digit. 
        /// </summary>
        /// <param name="arg">the argument token to inspect</param>
        /// <returns>true if the given token looks like a dash specified argument identifier</returns>
        public static bool IsDashSpecifiedArgumentIdentifier(string arg)
        {
            return arg.Length > 1 && arg[0] == '-' && char.IsDigit(arg[1]) == false;
        }

        internal static bool TryParseKey(string cmdLineArg, out string key)
        {
            if(IsDashSpecifiedArgumentIdentifier(cmdLineArg) == false && cmdLineArg.StartsWith("/") == false)
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
            else if (IsDashSpecifiedArgumentIdentifier(cmdLineArg))
            {
                string key = cmdLineArg.Substring(1);

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
                throw new ArgException("Could not parse key '"+cmdLineArg+"'");
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

        private static bool IsArrayOrList(string key, CommandLineArgumentsDefinition definition, ParseResult resultContext)
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

            return match.ArgumentType.IsArray || match.ArgumentType.GetInterfaces().Contains(typeof(IList));
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
