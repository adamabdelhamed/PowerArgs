using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;

namespace PowerArgs
{
    /// <summary>
    /// A hook that takes over the command line and provides tab completion for known strings when the user presses
    /// the tab key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TabCompletion : ArgHook
    {
        string indicator;
        Type completionSource;

        /// <summary>
        /// If this is > 0 then PowerArgs will save this many previous executions of the command line to your application data folder.
        /// Users can then access the history by pressing arrow up or down from the enhanced command prompt.
        /// </summary>
        public int HistoryToSave { get; set; }

        /// <summary>
        /// The location of the history file name (AppData/PowerArgs/EXE_NAME.TabCompletionHistory.txt
        /// </summary>
        public string HistoryFileName { get; set; }

        /// <summary>
        /// The name of your program (leave null and PowerArgs will try to detect it automatically)
        /// </summary>
        public string ExeName { get; set; }

        private string HistoryFileNameInternal
        {
            get
            {
                var exeName = ExeName ?? Path.GetFileName(Assembly.GetEntryAssembly().Location);
                return HistoryFileName ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PowerArgs", exeName + ".TabCompletionHistory.txt");
            }
        }

        /// <summary>
        /// Creates a new tab completion hook.
        /// </summary>
        /// <param name="indicator">When this indicator is the only argument the user specifies that triggers the hook to enhance the command prompt.  By default, the indicator is the empty string.</param>
        public TabCompletion(string indicator = "")
        {
            this.indicator = indicator;
            BeforeParsePriority = 100;
            HistoryToSave = 0;
        }

        /// <summary>
        /// Creates a new tab completion hook given a custom tab completion implementation.
        /// </summary>
        /// <param name="completionSource">A type that implements ITabCompletionSource such as SimpleTabCompletionSource</param>
        /// <param name="indicator">When this indicator is the only argument the user specifies that triggers the hook to enhance the command prompt.  By default, the indicator is the empty string.</param>
        public TabCompletion(Type completionSource, string indicator = "") : this(indicator)
        {
            if (completionSource.GetInterfaces().Contains(typeof(ITabCompletionSource)) == false)
            {
                throw new InvalidArgDefinitionException("Type " + completionSource + " does not implement " + typeof(ITabCompletionSource).Name);
            }

            this.completionSource = completionSource;
        }

        /// <summary>
        /// Before PowerArgs parses the args, this hook inspects the command line for the indicator and if found 
        /// takes over the command line and provides tab completion.
        /// </summary>
        /// <param name="context">The context used to inspect the command line arguments.</param>
        public override void BeforeParse(ArgHook.HookContext context)
        {
            if (indicator == "" && context.CmdLineArgs.Length != 0)return;
            else if (indicator != "" && (context.CmdLineArgs.Length != 1 || context.CmdLineArgs[0] != indicator)) return;

            try
            {
                // This is a little hacky, but I could not find a better way to make the tab completion start on the same lime
                // as the command line input

                var color = Console.ForegroundColor;
                var lastLine = ConsoleHelper.StdConsoleProvider.ReadALineOfConsoleOutput(Console.CursorTop - 1);
                Console.CursorTop--;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(lastLine);
                Console.ForegroundColor = color;
                Console.CursorTop--;
                Console.CursorLeft = lastLine.Length + 1;
            }
            catch (Exception)
            {
                Console.Write(indicator + "> ");
            }

            List<string> completions = FindTabCompletions(context.Args.GetType());

            List<ITabCompletionSource> completionSources = new List<ITabCompletionSource>();

            if(this.completionSource != null) completionSources.Add((ITabCompletionSource)Activator.CreateInstance(this.completionSource));
            completionSources.Add(new SimpleTabCompletionSource(completions) { MinCharsBeforeCyclingBegins = 0 });
            completionSources.Add(new FileSystemTabCompletionSource());

            string str = null;
            context.CmdLineArgs = ConsoleHelper.ReadLine(ref str, LoadHistory(), new MultiTabCompletionSource(completionSources));
            AddToHistory(str);
        }

        /// <summary>
        /// Clears all history saved on disk
        /// </summary>
        public void ClearHistory()
        {
            if (File.Exists(HistoryFileNameInternal))
            {
                File.WriteAllText(HistoryFileNameInternal, "");
            }
        }

        private void AddToHistory(string item)
        {
            if (HistoryToSave == 0) return;
            List<string> history = File.ReadAllLines(HistoryFileNameInternal).ToList();
            history.Insert(0, item);
            history = history.Distinct().ToList();
            while (history.Count > HistoryToSave) history.RemoveAt(history.Count - 1);
            File.WriteAllLines(HistoryFileNameInternal, history.ToArray());
        }

        private List<string> LoadHistory()
        {
            if (HistoryToSave == 0) return new List<string>();

            if (Directory.Exists(Path.GetDirectoryName(HistoryFileNameInternal)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryFileNameInternal));
                return new List<string>();
            }
            else if (File.Exists(HistoryFileNameInternal) == false)
            {
                File.WriteAllLines(HistoryFileNameInternal, new string[0]);
                return new List<string>();
            }
            else
            {
                return File.ReadAllLines(HistoryFileNameInternal).ToList();
            }
        }

        private List<string> FindTabCompletions(Type t)
        {
            List<string> ret = new List<string>();

            var argIndicator = "-";

            ret.AddRange(from a in t.GetArguments() select argIndicator + a.GetArgumentName());

            foreach (var actionArg in t.GetActionArgProperties())
            {
                var caseMatters = (actionArg.HasAttr<ArgIgnoreCase>() && !actionArg.Attr<ArgIgnoreCase>().IgnoreCase) ||
                 (actionArg.DeclaringType.HasAttr<ArgIgnoreCase>() && !actionArg.DeclaringType.Attr<ArgIgnoreCase>().IgnoreCase);

                if (caseMatters) ret.Add(actionArg.Name.Substring(0,actionArg.Name.Length - Constants.ActionArgConventionSuffix.Length));
                else             ret.Add(actionArg.Name.Substring(0,actionArg.Name.Length - Constants.ActionArgConventionSuffix.Length).ToLower());
   
                ret.AddRange(FindTabCompletions(actionArg.PropertyType));
            }

 

            ret = ret.Distinct().ToList();
            return ret;
        }

    }

    /// <summary>
    /// An interface used to implement custom tab completion logic.
    /// </summary>
    public interface ITabCompletionSource
    {
        /// <summary>
        /// PowerArgs will call this method if it has enhanced the command prompt and the user presses tab.  You should use the
        /// text the user has types so far to determine if there is a completion you'd like to make.  If you find a completion
        /// then you should assign it to the completion variable and return true.
        /// </summary>
        /// <param name="shift">Indicates if shift was being pressed</param>
        /// <param name="soFar">The text token that the user has typed before pressing tab.</param>
        /// <param name="completion">The variable that you should assign the completed string to if you find a match.</param>
        /// <returns>True if you completed the string, false otherwise.</returns>
        bool TryComplete(bool shift, string soFar, out string completion);
    }

    /// <summary>
    /// An aggregate tab completion source that cycles through it's inner sources looking for matches.
    /// </summary>
    public class MultiTabCompletionSource : ITabCompletionSource
    {
        ITabCompletionSource[] sources;

        /// <summary>
        /// Create a new MultiTabCompletionSource given an array of sources.
        /// </summary>
        /// <param name="sources">The sources to wrap</param>
        public MultiTabCompletionSource(params ITabCompletionSource[] sources)
        {
            this.sources = sources;
        }

        /// <summary>
        /// Create a new MultiTabCompletionSource given an IEnumerable of sources.
        /// </summary>
        /// <param name="sources">The sources to wrap</param>
        public MultiTabCompletionSource(IEnumerable<ITabCompletionSource> sources)
        {
            this.sources = sources.ToArray();
        }

        /// <summary>
        /// Iterates over the wrapped sources looking for a match
        /// </summary>
        /// <param name="shift">Indicates if shift was being pressed</param>
        /// <param name="soFar">The text token that the user has typed before pressing tab.</param>
        /// <param name="completion">The variable that you should assign the completed string to if you find a match.</param>
        /// <returns></returns>
        public bool TryComplete(bool shift, string soFar, out string completion)
        {
            foreach (var source in sources)
            {
                if (source.TryComplete(shift, soFar, out completion)) return true;
            }
            completion = null;
            return false;
        }
    }

    /// <summary>
    /// A simple tab completion source implementation that looks for matches over a set of pre-determined strings.
    /// </summary>
    public class SimpleTabCompletionSource : ITabCompletionSource
    {
        IEnumerable<string> candidates;

        string lastSoFar;
        string lastCompletion;
        int lastIndex;

        /// <summary>
        /// Require that the user type this number of characters before the source starts cycling through ambiguous matches.  The default is 3.
        /// </summary>
        public int MinCharsBeforeCyclingBegins { get; set; }

        /// <summary>
        /// Creates a new completion source given an enumeration of string candidates
        /// </summary>
        /// <param name="candidates"></param>
        public SimpleTabCompletionSource(IEnumerable<string> candidates)
        {
            this.candidates = candidates.OrderBy(s => s);
            this.MinCharsBeforeCyclingBegins = 3;
        }

        /// <summary>
        /// Iterates through the candidates to try to find a match.  If there are multiple possible matches it 
        /// supports cycling through tem as the user continually presses tab.
        /// </summary>
        /// <param name="shift">Indicates if shift was being pressed</param>
        /// <param name="soFar">The text token that the user has typed before pressing tab.</param>
        /// <param name="completion">The variable that you should assign the completed string to if you find a match.</param>
        /// <returns></returns>
        public bool TryComplete(bool shift, string soFar, out string completion)
        {
            if (soFar == lastCompletion && lastCompletion != null)
            {
                soFar = lastSoFar;
            }

            var query = (from c in candidates where c.StartsWith(soFar) select c).ToList();

            if (soFar == lastSoFar) lastIndex = shift ? lastIndex-1 : lastIndex+1;
            if (lastIndex >= query.Count) lastIndex = 0;
            if (lastIndex < 0) lastIndex = query.Count - 1;
            lastSoFar = soFar;

            if (query.Count == 0 || (query.Count > 1 && soFar.Length < MinCharsBeforeCyclingBegins))
            {
                completion = null;
                return false;
            }
            else
            {
                completion = query[lastIndex];
                lastCompletion = completion;
                return true;
            }
        }
    }

    internal class FileSystemTabCompletionSource : ITabCompletionSource
    {
        string lastSoFar = null, lastCompletion = null;
        int tabIndex = -1;
        public bool TryComplete(bool shift, string soFar, out string completion)
        {
            completion = null;
            try
            {
                soFar = soFar.Replace("\"", "");
                if (soFar == "")
                {
                    soFar = lastSoFar ?? ".\\";
                }

                if (soFar == lastCompletion)
                {
                    soFar = lastSoFar;
                }
                else
                {
                    tabIndex = -1;
                }

                var dir = Path.GetDirectoryName(soFar);

                if (Path.IsPathRooted(soFar) == false)
                {
                    dir = Environment.CurrentDirectory;
                    soFar = ".\\" + soFar;
                }

                if (Directory.Exists(dir) == false)
                {
                    return false;
                }
                var rest = Path.GetFileName(soFar);

                var matches = from f in Directory.GetFiles(dir)
                              where f.ToLower().StartsWith(Path.GetFullPath(soFar).ToLower())
                              select f;

                var matchesArray = (matches.Union(from d in Directory.GetDirectories(dir)
                                                  where d.ToLower().StartsWith(Path.GetFullPath(soFar).ToLower())
                                                  select d)).ToArray();

                if (matchesArray.Length > 0)
                {
                    tabIndex = shift ? tabIndex - 1 : tabIndex + 1;
                    if (tabIndex < 0) tabIndex = matchesArray.Length - 1;
                    if (tabIndex >= matchesArray.Length) tabIndex = 0;

                    completion = matchesArray[tabIndex];

                    if (completion.Contains(" "))
                    {
                        completion = '"' + completion + '"';
                    }
                    lastSoFar = soFar;
                    lastCompletion = completion.Replace("\"", "");
                    return true;
                }
                else
                {
                    lastSoFar = null;
                    lastCompletion = null;
                    tabIndex = -1;
                    return false;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;  // We don't want a bug in this logic to break the app
            }
        }
    }

    /// <summary>
    /// Used for internal implementation, but marked public for testing, please do not use.
    /// </summary>
    public static class ConsoleHelper
    {
        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        public interface IConsoleProvider
        {
            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            int CursorLeft { get; set; }

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            ConsoleKeyInfo ReadKey();

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            void Write(object output);

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            void WriteLine(object output);

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            void WriteLine();
        }

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        public static IConsoleProvider ConsoleImpl = new StdConsoleProvider();

        /// <summary>
        /// Used for internal implementation, but marked public for testing, please do not use.
        /// </summary>
        public class StdConsoleProvider : IConsoleProvider
        {
            const int STD_OUTPUT_HANDLE = -11;

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            public int CursorLeft
            {
                get
                {
                    return Console.CursorLeft;
                }
                set
                {
                    Console.CursorLeft = value;
                }
            }

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            /// <returns>Used for internal implementation, but marked public for testing, please do not use.</returns>
            public ConsoleKeyInfo ReadKey()
            {
                return Console.ReadKey(true);
            }

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            /// <param name="output">Used for internal implementation, but marked public for testing, please do not use.</param>
            public void Write(object output)
            {
                Console.Write(output);
            }

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            /// <param name="output">Used for internal implementation, but marked public for testing, please do not use.</param>
            public void WriteLine(object output)
            {
                Console.WriteLine(output);
            }

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            public void WriteLine()
            {
                Console.WriteLine();
            }

            [DllImport("Kernel32", SetLastError = true)]
            static extern IntPtr GetStdHandle(int nStdHandle);

            [DllImport("Kernel32", SetLastError = true)]
            static extern bool ReadConsoleOutputCharacter(IntPtr hConsoleOutput,
                [Out] StringBuilder lpCharacter, uint nLength, COORD dwReadCoord,
                out uint lpNumberOfCharsRead);

            [StructLayout(LayoutKind.Sequential)]
            struct COORD
            {
                public short X;
                public short Y;
            }

            /// <summary>
            /// Used for internal implementation, but marked public for testing, please do not use.
            /// </summary>
            /// <param name="y">Used for internal implementation, but marked public for testing, please do not use.</param>
            /// <returns>Used for internal implementation, but marked public for testing, please do not use.</returns>
            public static string ReadALineOfConsoleOutput(int y)
            {
                if (y < 0) throw new Exception();
                IntPtr stdout = GetStdHandle(STD_OUTPUT_HANDLE);

                uint nLength = (uint)Console.WindowWidth;
                StringBuilder lpCharacter = new StringBuilder((int)nLength);

                // read from the first character of the first line (0, 0).
                COORD dwReadCoord;
                dwReadCoord.X = 0;
                dwReadCoord.Y = (short)y;

                uint lpNumberOfCharsRead = 0;

                if (!ReadConsoleOutputCharacter(stdout, lpCharacter, nLength, dwReadCoord, out lpNumberOfCharsRead))
                    throw new Win32Exception();

                var str = lpCharacter.ToString();
                str = str.Substring(0, str.Length - 1).Trim();

                return str;
            }
        }

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

        private static void RefreshConsole(int leftStart, List<char> chars, int offset = 0, int lookAhead = 1)
        {
            int left = ConsoleImpl.CursorLeft;
            ConsoleImpl.CursorLeft = leftStart;
            for (int i = 0; i < chars.Count; i++) ConsoleImpl.Write(chars[i]);
            for(int i = 0; i < lookAhead; i++) ConsoleImpl.Write(" ");
            ConsoleImpl.CursorLeft = left + offset;
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

        internal static string[] ReadLine(ref string rawInput, List<string> history, params ITabCompletionSource[] tabCompletionHooks)
        {
            var leftStart = ConsoleImpl.CursorLeft;
            var chars = new List<char>();

            int historyIndex = -1;
            history = history ?? new List<string>();

            while (true)
            {
                var info = ConsoleImpl.ReadKey();
                int i = ConsoleImpl.CursorLeft - leftStart;

                if (info.Key == ConsoleKey.Home)
                {
                    ConsoleImpl.CursorLeft = leftStart;
                    continue;
                }
                else if (info.Key == ConsoleKey.End)
                {
                    ConsoleImpl.CursorLeft = leftStart + chars.Count;
                    continue;
                }
                else if (info.Key == ConsoleKey.UpArrow)
                {
                    if (history.Count == 0) continue;
                    ConsoleImpl.CursorLeft = leftStart;
                    historyIndex++;
                    if (historyIndex >= history.Count) historyIndex = 0;
                    chars = history[historyIndex].ToList();
                    RefreshConsole(leftStart, chars, chars.Count);
                    continue;
                }
                else if (info.Key == ConsoleKey.DownArrow)
                {
                    if (history.Count == 0) continue;
                    ConsoleImpl.CursorLeft = leftStart;
                    historyIndex--;
                    if (historyIndex < 0) historyIndex = history.Count - 1;
                    chars = history[historyIndex].ToList();
                    RefreshConsole(leftStart, chars, chars.Count);
                    continue;
                }
                else if (info.Key == ConsoleKey.LeftArrow)
                {
                    ConsoleImpl.CursorLeft = Math.Max(leftStart, ConsoleImpl.CursorLeft - 1);
                    continue;
                }
                else if (info.Key == ConsoleKey.RightArrow)
                {
                    ConsoleImpl.CursorLeft = Math.Min(ConsoleImpl.CursorLeft + 1, leftStart + chars.Count);
                    continue;
                }
                else if (info.Key == ConsoleKey.Delete)
                {
                    if (i < chars.Count)
                    {
                        chars.RemoveAt(i);
                        RefreshConsole(leftStart, chars);
                    }
                    continue;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (i == 0) continue;
                    i--;
                    ConsoleImpl.CursorLeft = ConsoleImpl.CursorLeft - 1;
                    if (i < chars.Count)
                    {
                        chars.RemoveAt(i);
                        RefreshConsole(leftStart, chars);
                    }
                    continue;
                }
                else if (info.Key == ConsoleKey.Enter)
                {
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

                    token = new string(token.Reverse().ToArray());

                    string completion = null;

                    foreach (var completionSource in tabCompletionHooks)
                    {
                        if (completionSource.TryComplete(info.Modifiers.HasFlag(ConsoleModifiers.Shift),  token, out completion)) break;
                    }

                    if (completion == null) continue;

                    var insertThreshold = j + token.Length;

                    for (int k = 0; k < completion.Length; k++)
                    {
                        if (k + j == chars.Count)
                        {
                            chars.Add(completion[k]);
                        }
                        else if (k + j < insertThreshold)
                        {
                            chars[k + j] = completion[k];
                        }
                        else
                        {
                            chars.Insert(k + j, completion[k]);
                        }
                    }

                    // Handle the case where the completion is smaller than the token
                    int extraChars = token.Length - completion.Length;
                    for (int k = 0; k < extraChars; k++)
                    {
                        chars.RemoveAt(j + completion.Length);
                    }

                    RefreshConsole(leftStart, chars, completion.Length - token.Length, extraChars);
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
                        RefreshConsole(leftStart, chars, 1);
                    }
                    continue;
                }
            }

            rawInput = new string(chars.ToArray());

            return GetArgs(chars);
        }
    }
}
