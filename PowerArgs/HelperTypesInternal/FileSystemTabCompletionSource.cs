using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PowerArgs
{
    internal class FileSystemTabCompletionSource : ITabCompletionSource
    {
        string lastSoFar = null, lastCompletion = null;
        int tabIndex = -1;
        public bool TryComplete(TabCompletionContext context, out string completion)
        {
            completion = null;
            try
            {
                context.CompletionCandidate = context.CompletionCandidate.Replace("\"", "");
                if (context.CompletionCandidate == "")
                {
                    context.CompletionCandidate = lastSoFar ?? ".\\";
                }

                if (context.CompletionCandidate == lastCompletion)
                {
                    context.CompletionCandidate = lastSoFar;
                }
                else
                {
                    tabIndex = -1;
                }

                var dir = Path.GetDirectoryName(context.CompletionCandidate);

                if (Path.IsPathRooted(context.CompletionCandidate) == false)
                {
                    dir = Environment.CurrentDirectory;
                    context.CompletionCandidate = ".\\" + context.CompletionCandidate;
                }

                if (Directory.Exists(dir) == false)
                {
                    return false;
                }
                var rest = Path.GetFileName(context.CompletionCandidate);

                var matches = from f in Directory.GetFiles(dir)
                              where f.ToLower().StartsWith(Path.GetFullPath(context.CompletionCandidate).ToLower())
                              select f;

                var matchesArray = (matches.Union(from d in Directory.GetDirectories(dir)
                                                  where d.ToLower().StartsWith(Path.GetFullPath(context.CompletionCandidate).ToLower())
                                                  select d)).ToArray();

                if (matchesArray.Length > 0)
                {
                    tabIndex = context.Shift ? tabIndex - 1 : tabIndex + 1;
                    if (tabIndex < 0) tabIndex = matchesArray.Length - 1;
                    if (tabIndex >= matchesArray.Length) tabIndex = 0;

                    completion = matchesArray[tabIndex];

                    if (completion.Contains(" "))
                    {
                        completion = '"' + completion + '"';
                    }
                    lastSoFar = context.CompletionCandidate;
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
                // TODO P2 - Why do we have tracing here?
                Trace.TraceError(ex.ToString());
                return false;  // We don't want a bug in this logic to break the app
            }
        }
    }
}
