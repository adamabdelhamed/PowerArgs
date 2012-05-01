using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace PowerArgs
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TabCompletion : ArgHook
    {
        string indicator;
        Type completionSource;
        public TabCompletion(string indicator = "")
        {
            this.indicator = indicator;
            BeforeParsePriority = 100;
        }

        public TabCompletion(Type completionSource, string indicator = "") : this(indicator)
        {
            if (completionSource.GetInterfaces().Contains(typeof(ITabCompletionSource)) == false)
            {
                throw new InvalidArgDefinitionException("Type " + completionSource + " does not implement " + typeof(ITabCompletionSource).Name);
            }

            this.completionSource = completionSource;
        }

        public override void BeforeParse(ArgHook.HookContext context)
        {
            if (indicator == "" && context.CmdLineArgs.Length != 0)return;
            else if (indicator != "" && (context.CmdLineArgs.Length != 1 || context.CmdLineArgs[0] != indicator)) return;

            Console.WriteLine();
            Console.Write(indicator + "> ");

            List<string> completions = FindTabCompletions(context.Args.GetType());
            context.CmdLineArgs = ConsoleHelper.ReadLine(new SimpleTabCompletionSource(completions));
        }

        private List<string> FindTabCompletions(Type t)
        {
            List<string> ret = new List<string>();

            var argIndicator =t.GetArgStyle() == ArgStyle.PowerShell ? "-" : "/";
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

    public interface ITabCompletionSource
    {
        bool TryComplete(string soFar, out string completion);
    }

    public class SimpleTabCompletionSource : ITabCompletionSource
    {
        IEnumerable<string> candidates;
        public SimpleTabCompletionSource(IEnumerable<string> candidates)
        {
            this.candidates = candidates;
        }
        public bool TryComplete(string soFar, out string completion)
        {
            var query = from c in candidates where c.StartsWith(soFar) select c;
            if (query.Count() != 1)
            {
                completion = null;
                return false;
            }
            else
            {
                completion = query.First();
                return true;
            }
        }
    }

    public static class ConsoleHelper
    {
        public interface IConsoleProvider
        {
            int CursorLeft { get; set; }
            ConsoleKeyInfo ReadKey();
            void Write(object output);
            void WriteLine(object output);
            void WriteLine();
        }

        public class StdConsoleProvider : IConsoleProvider
        {
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

            public ConsoleKeyInfo ReadKey()
            {
                return Console.ReadKey(true);
            }

            public void Write(object output)
            {
                Console.Write(output);
            }

            public void WriteLine(object output)
            {
                Console.WriteLine(output);
            }

            public void WriteLine()
            {
                Console.WriteLine();
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
                if (char.IsWhiteSpace(c))
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


        public static IConsoleProvider ConsoleImpl = new StdConsoleProvider();

        private static void RefreshConsole(int leftStart, List<char> chars, int offset = 0)
        {
            int left = ConsoleImpl.CursorLeft;
            ConsoleImpl.CursorLeft = leftStart;
            for (int i = 0; i < chars.Count; i++) ConsoleImpl.Write(chars[i]);
            ConsoleImpl.Write(" ");
            ConsoleImpl.CursorLeft = left + offset;
        }

        public static string[] ReadLine(params ITabCompletionSource[] tabCompletionHooks)
        {
            var leftStart = ConsoleImpl.CursorLeft;
            var chars = new List<char>();

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

                    int j;
                    for (j = i - 1; j >= 0; j--)
                    {
                        if (chars[j] == ' ')
                        {
                            j++;
                            break;
                        }
                        else token += chars[j];
                    }


                    if (j == -1) j = 0;

                    if (token.Length == 0) continue;

                    token = new string(token.Reverse().ToArray());

                    string completion = null;

                    foreach (var completionSource in tabCompletionHooks)
                    {
                        if (completionSource.TryComplete(token, out completion)) break;
                    }

                    if (completion == null) continue;

                    int soFar = i - j;
                    for (int k = soFar; k < completion.Length; k++)
                    {
                        if (k + j == chars.Count)
                        {
                            chars.Add(completion[k]);
                        }
                        else
                        {
                            chars.Insert(k + j, completion[k]);
                        }
                    }

                    RefreshConsole(leftStart, chars, completion.Length - soFar);
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

            return GetArgs(chars);
        }
    }
}
