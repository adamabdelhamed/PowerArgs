using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// Used for internal implementation, but marked public for testing, please do not use.
    /// </summary>
    public static class ConsoleHelper
    {


        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        public static IConsoleProvider ConsoleImpl = new StdConsoleProvider();



        private static string[] GetArgs(List<char> chars)
        {
            List<string> ret = new List<string>();

            bool inQuotes = false;
            string token = "";
            for (int i = 0; i < chars.Count; i++)
            {
                char c = chars[i];
                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    ret.Add(token);
                    token = "";
                }
                else if (c == '\\' && i < chars.Count - 1 && chars[i + 1] == '"')
                {
                    token += '"';
                    i++;
                }
                else if (c == '"')
                {
                    if (!inQuotes)
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        ret.Add(token);
                        token = "";
                        inQuotes = false;
                    }
                }
                else
                {
                    token += c;
                }
            }

            ret.Add(token);

            return (from t in ret where string.IsNullOrWhiteSpace(t) == false select t.Trim()).ToArray();
        }

        private static void RefreshConsole(int leftStart, int topStart, List<char> chars, int leftAdjust, int topAdjust)
        {
            int left = ConsoleImpl.CursorLeft;
            var top = ConsoleImpl.CursorTop;
            ConsoleImpl.CursorLeft = leftStart;
            ConsoleImpl.CursorTop = topStart;
            for (int i = 0; i < chars.Count; i++) ConsoleImpl.Write(chars[i]);
            ConsoleImpl.Write(" ");
            ConsoleImpl.Write(" ");

            var desiredLeft = left + leftAdjust;

            if(desiredLeft == ConsoleImpl.BufferWidth)
            {
                ConsoleImpl.CursorLeft = top == topStart ? leftStart : 0;
                ConsoleImpl.CursorTop = top+1;
            }
            else if(desiredLeft == -1)
            {
                ConsoleImpl.CursorLeft = ConsoleImpl.BufferWidth-1;
                ConsoleImpl.CursorTop = top - 1;
            }
            else
            {
                ConsoleImpl.CursorLeft = desiredLeft;
                ConsoleImpl.CursorTop = top;
            }

   
        }

        private static void ReplaceConsole(int leftStart, int topStart, List<char> oldChars, List<char> newChars)
        {
            ConsoleImpl.CursorLeft = leftStart;
            ConsoleImpl.CursorTop = topStart;
            for (int i = 0; i < newChars.Count; i++) ConsoleImpl.Write(newChars[i]);

            var newLeft = ConsoleImpl.CursorLeft;
            var newTop = ConsoleImpl.CursorTop;

            for (int i = 0; i < oldChars.Count - newChars.Count; i++ )
            {
                ConsoleImpl.Write(" ");
            }

            ConsoleImpl.CursorTop = newTop;
            ConsoleImpl.CursorLeft = newLeft;
        }

        private enum QuoteStatus
        {
            OpenedQuote,
            ClosedQuote,
            NoQuotes
        }

        private static QuoteStatus GetQuoteStatus(List<char> chars, int startPosition)
        {
            bool open = false;

            for (int i = 0; i <= startPosition; i++)
            {
                var c = chars[i];
                if (i > 0 && c == '"' && chars[i - 1] == '\\')
                {
                    // escaped
                }
                else if (c == '"')
                {
                    open = !open;
                }
            }

            if (open) return QuoteStatus.OpenedQuote;

            var charsAsString = new string(chars.ToArray());

            if (chars.LastIndexOf('"') > chars.LastIndexOf(' ')) return QuoteStatus.ClosedQuote;

            return QuoteStatus.NoQuotes;

        }

        private static List<string> Tokenize(string chars)
        {
            bool open = false;

            List<string> ret = new List<string>();
            string currentToken = "";

            for (int i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (i > 0 && c == '"' && chars[i - 1] == '\\')
                {

                }
                else if (c == '"')
                {
                    open = !open;
                    if (!open)
                    {
                        ret.Add(currentToken);
                        currentToken = "";
                        continue;
                    }
                }

                if (c != ' ' || open)
                {
                    currentToken += c;
                }
                else if (c == ' ')
                {
                    ret.Add(currentToken);
                    currentToken = "";
                }
            }

            if (open) return null;

            return ret.Where(token => string.IsNullOrWhiteSpace(token) == false).ToList();

        }

        /// <summary>
        /// Only marked public for testing.  Please do not use.
        /// </summary>
        /// <param name="rawInput">a reference to the command line string</param>
        /// <param name="history">Any previous command lines</param>
        /// <param name="definition">The args definition</param>
        /// <returns>A new set of command line args</returns>
        public static string[] ReadLine(ref string rawInput, List<string> history, CommandLineArgumentsDefinition definition)
        {
            IEnumerable<ITabCompletionSource> oldHooks = FindOldTabCompletionHooks(definition);
            IEnumerable<ISmartTabCompletionSource> newHooks = FindNewTabCompletionHooks(definition);

            var leftStart = ConsoleImpl.CursorLeft;
            var topStart = ConsoleImpl.CursorTop;
            var chars = new List<char>();

            int historyIndex = -1;
            history = history ?? new List<string>();

            while (true)
            {
                var info = ConsoleImpl.ReadKey(true);
                int i = ConsoleImpl.CursorLeft - leftStart + (ConsoleImpl.CursorTop - topStart) * ConsoleImpl.BufferWidth;

                if (info.Key == ConsoleKey.Home)
                {
                    ConsoleImpl.CursorTop = topStart;
                    ConsoleImpl.CursorLeft = leftStart;
                    continue;
                }
                else if (info.Key == ConsoleKey.End)
                {
                    ConsoleImpl.CursorTop = topStart + (int)(Math.Floor((leftStart + chars.Count) / (double)ConsoleImpl.BufferWidth));
                    ConsoleImpl.CursorLeft = (leftStart + chars.Count) % ConsoleImpl.BufferWidth;
                    continue;
                }
                else if (info.Key == ConsoleKey.UpArrow)
                {
                    if (history.Count == 0) continue;
                    ConsoleImpl.CursorLeft = leftStart;
                    historyIndex++;
                    if (historyIndex >= history.Count) historyIndex = 0;
                    var newChars = history[historyIndex].ToList();
                    ReplaceConsole(leftStart, topStart, chars, newChars);
                    chars = newChars;
                    continue;
                }
                else if (info.Key == ConsoleKey.DownArrow)
                {
                    if (history.Count == 0) continue;
                    ConsoleImpl.CursorLeft = leftStart;
                    historyIndex--;
                    if (historyIndex < 0) historyIndex = history.Count - 1;
                    var newChars = history[historyIndex].ToList();
                    ReplaceConsole(leftStart, topStart, chars, newChars);
                    chars = newChars;
                    continue;
                }
                else if (info.Key == ConsoleKey.LeftArrow)
                {
                    if (ConsoleImpl.CursorTop == topStart && ConsoleImpl.CursorLeft > leftStart)
                    {
                        ConsoleImpl.CursorLeft -= 1;
                    }
                    else if (ConsoleImpl.CursorLeft > 0)
                    {
                        ConsoleImpl.CursorLeft -= 1;
                    }
                    else if (ConsoleImpl.CursorTop > topStart)
                    {
                        ConsoleImpl.CursorTop--;
                        ConsoleImpl.CursorLeft = ConsoleImpl.BufferWidth - 1;
                    }

                    continue;
                }
                else if (info.Key == ConsoleKey.RightArrow)
                {
                    if (ConsoleImpl.CursorLeft < ConsoleImpl.BufferWidth - 1 && i < chars.Count)
                    {
                        ConsoleImpl.CursorLeft = ConsoleImpl.CursorLeft + 1;
                    }
                    else if (ConsoleImpl.CursorLeft == ConsoleImpl.BufferWidth - 1)
                    {
                        ConsoleImpl.CursorTop++;
                        ConsoleImpl.CursorLeft = 0;
                    }

                    continue;
                }
                else if (info.Key == ConsoleKey.Delete)
                {
                    if (i < chars.Count)
                    {
                        chars.RemoveAt(i);
                        RefreshConsole(leftStart, topStart, chars, 0, 0);
                    }
                    continue;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (i == 0) continue;
                    i--;
 
                    if (i < chars.Count)
                    {
                        chars.RemoveAt(i);
                        RefreshConsole(leftStart, topStart, chars, -1, 0);
                    }

                    continue;
                }
                else if (info.Key == ConsoleKey.Enter)
                {
                    ConsoleImpl.CursorTop = topStart + (int)(Math.Floor((leftStart + chars.Count) / (double)ConsoleImpl.BufferWidth));
                    ConsoleImpl.CursorLeft = (leftStart + chars.Count) % ConsoleImpl.BufferWidth;
                    ConsoleImpl.WriteLine();
                    break;
                }
                else if (info.Key == ConsoleKey.Tab)
                {
                    var token = "";
                    var quoteStatus = GetQuoteStatus(chars, i - 1);
                    bool readyToEnd = quoteStatus != QuoteStatus.ClosedQuote;
                    var endTarget = quoteStatus == QuoteStatus.NoQuotes ? ' ' : '"';

                    int j;
                    for (j = i - 1; j >= 0; j--)
                    {
                        if (chars[j] == endTarget && readyToEnd)
                        {
                            if (endTarget == ' ')
                            {
                                j++;
                            }
                            else
                            {
                                token += chars[j];
                            }

                            break;
                        }
                        else if (chars[j] == endTarget)
                        {
                            token += chars[j];
                            readyToEnd = true;
                        }
                        else
                        {
                            token += chars[j];
                        }
                    }

                    if (j == -1) j = 0;

                    var previousToken = "";
                    for (int k = j - 1; k >= 0; k--)
                    {
                        previousToken += chars[k];
                    }
                    previousToken = new string(previousToken.Reverse().ToArray());
                    previousToken = ParseContext(previousToken);

                    token = new string(token.Reverse().ToArray());

                    string completion = null;

                    var tabCompletionContext = GenerateTabCompletionContext(definition,new String(chars.ToArray()), info.Modifiers.HasFlag(ConsoleModifiers.Shift), previousToken, token);

                    bool oldHookWon = false;
                    foreach (var completionSource in oldHooks)
                    {
                        if (completionSource is ITabCompletionSourceWithContext)
                        {
                            if (((ITabCompletionSourceWithContext)completionSource).TryComplete(info.Modifiers.HasFlag(ConsoleModifiers.Shift), previousToken, token, out completion))
                            {
                                oldHookWon = true;
                                break;
                            }
                        }
                        else
                        {
                            if (completionSource.TryComplete(info.Modifiers.HasFlag(ConsoleModifiers.Shift), token, out completion))
                            {
                                oldHookWon = true;
                                break;
                            }
                        }
                    }

                    if(oldHookWon == false)
                    {
                        var context = GenerateTabCompletionContext(definition, ToString(chars), info.Modifiers.HasFlag(ConsoleModifiers.Shift), previousToken, token);
                        foreach(var completionSource in newHooks)
                        {
                            if(completionSource.TryComplete(context, out completion))
                            {
                                break;
                            }
                        }
                    }

                    if (completion == null) continue;

                    if (completion.Contains(" ") && completion.StartsWith("\"") == false && completion.EndsWith("\"") == false)
                    {
                        completion = '"' + completion + '"';
                    }

                    var insertThreshold = j + token.Length;

                    var newChars = new List<char>(chars);
                    for (int k = 0; k < completion.Length; k++)
                    {
                        if (k + j == newChars.Count)
                        {
                            newChars.Add(completion[k]);
                        }
                        else if (k + j < insertThreshold)
                        {
                            newChars[k + j] = completion[k];
                        }
                        else
                        {
                            newChars.Insert(k + j, completion[k]);
                        }
                    }


                    while (newChars.Count > j + completion.Length)
                    {
                        newChars.RemoveAt(j + completion.Length);
                    }

                    var left = ConsoleImpl.CursorLeft;
                    ReplaceConsole(leftStart, topStart, chars, newChars);
 
                    chars = newChars;
                }
                else
                {
                    if (i == chars.Count)
                    {
                        chars.Add(info.KeyChar);
                        ConsoleImpl.Write(info.KeyChar);
                    }
                    else
                    {
                        chars.Insert(i, info.KeyChar);
                        RefreshConsole(leftStart, topStart, chars, 1, 0);
                    }
                    continue;
                }
            }

            rawInput = new string(chars.ToArray());

            return GetArgs(chars);
        }

        private static string ToString(IEnumerable<char> chars)
        {
            var ret = "";
            foreach(var c in chars)
            {
                ret += c;
            }
            return ret;
        }

        private static IEnumerable<ITabCompletionSource> FindOldTabCompletionHooks(CommandLineArgumentsDefinition definition)
        {
            List<ITabCompletionSource> completionSources = new List<ITabCompletionSource>();

            if (definition.Metadata.HasMeta<TabCompletion>() && definition.Metadata.Meta<TabCompletion>().CompletionSourceType != null && definition.Metadata.Meta<TabCompletion>().CompletionSourceType.GetInterfaces().Contains(typeof(ITabCompletionSource)))
            {
                completionSources.Add((ITabCompletionSource)Activator.CreateInstance(definition.Metadata.Meta<TabCompletion>().CompletionSourceType));
            }

            foreach (var argument in definition.AllGlobalAndActionArguments)
            {
                foreach (var argSource in argument.Metadata.Metas<ArgumentAwareTabCompletionAttribute>())
                {
                    var source = argSource.CreateTabCompletionSource(definition, argument);
                    if (source is ITabCompletionSource)
                    {
                        completionSources.Insert(0, (ITabCompletionSource)source);
                    }
                }
            }

            return completionSources;
        }

        private static IEnumerable<ISmartTabCompletionSource> FindNewTabCompletionHooks(CommandLineArgumentsDefinition definition)
        {
            List<ISmartTabCompletionSource> completionSources = new List<ISmartTabCompletionSource>();

            if (definition.Metadata.HasMeta<TabCompletion>() && definition.Metadata.Meta<TabCompletion>().CompletionSourceType != null && definition.Metadata.Meta<TabCompletion>().CompletionSourceType.GetInterfaces().Contains(typeof(ISmartTabCompletionSource)))
            {
                completionSources.Add((ISmartTabCompletionSource)Activator.CreateInstance(definition.Metadata.Meta<TabCompletion>().CompletionSourceType));
            }

            foreach (var argument in definition.AllGlobalAndActionArguments)
            {
                foreach (var argSource in argument.Metadata.Metas<ArgumentAwareTabCompletionAttribute>())
                {
                    var source = argSource.CreateTabCompletionSource(definition, argument);
                    if (source is ISmartTabCompletionSource)
                    {
                        completionSources.Insert(0, (ISmartTabCompletionSource)source);
                    }
                }

                if (argument.ArgumentType.IsEnum)
                {
                    completionSources.Insert(0, new EnumTabCompletionSource(argument));
                }
            }
            completionSources.Add(new ActionAndArgumentSmartTabCompletionSource());
            completionSources.Add(new FileSystemTabCompletionSource());

            return completionSources;
        }

        private static TabCompletionContext GenerateTabCompletionContext(CommandLineArgumentsDefinition definition, string commandLine, bool shift, string previousToken, string completionCandidate)
        {
            TabCompletionContext context = new TabCompletionContext();
            context.Definition = definition;
            context.Shift = shift;
            context.PreviousToken = previousToken;
            context.CompletionCandidate = completionCandidate;
            context.CommandLineText = commandLine;

            var firstToken = commandLine.Split(' ').FirstOrDefault();
            if(firstToken != null)
            {
                var match = (from a in definition.Actions where a.IsMatch(firstToken) select a).SingleOrDefault();
                if(match != null)
                {
                    context.TargetAction = match;
                }
            }

            string argumentMatchId = null;
            if (previousToken.StartsWith("-"))
            {
                argumentMatchId = previousToken.Substring(1);
            }
            else if (previousToken.StartsWith("/"))
            {
                argumentMatchId = previousToken.Substring(1);
            }

            if (argumentMatchId != null)
            {
                var match = definition.Arguments.Where(arg => arg.IsMatch(argumentMatchId) && arg.ArgumentType != typeof(bool)).SingleOrDefault();

                if (match == null && context.TargetAction != null)
                {
                    match = context.TargetAction.Arguments.Where(arg => arg.IsMatch(argumentMatchId) && arg.ArgumentType != typeof(bool)).SingleOrDefault();
                }

                if(match != null)
                {
                    context.TargetArgument = match;
                }
            }

            return context;
        }

        /// <summary>
        /// The input is the full command line previous to the token to be completed.  This function 
        /// pulls out the last token before the completion's 'so far' input.
        /// </summary>
        /// <param name="commandLine"></param>
        /// <returns></returns>
        private static string ParseContext(string commandLine)
        {
            var tokens = ConsoleHelper.Tokenize(commandLine);
            if (tokens == null) return "";
            else if (tokens.Count == 0) return "";
            else return tokens.Last();
        }
    }
}
