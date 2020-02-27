using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Samples
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling), ArgDescription("A sample that uses the familiar git command line tool to show how to implement programs with multiple actions."), TabCompletion(ExeName="git", HighlighterConfiguratorType=typeof(CustomHighlighter)), /*[TabCompletion] is useful for the sample, but you don't need it in your program (unless you want it).*/ ]
    public class GitExampleArgs
    {
        [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help documentation")]
        public bool Help { get; set; }

        [ArgActionMethod, ArgDescription("Push your local changes to a remote repo")]
        [ArgExample("git push origin master", "pushes committed changes to the master branch to the remote named 'origin'", Title = "Push to a remote")]
        public void Push([ArgRequired, ArgDescription("The name of the remote to push to")]string remote, [DefaultValue("master"), ArgDescription("The name of the branch to push")]string branch)
        {
            Console.WriteLine("Pushing to " + remote + ", branch=" + branch);
        }

        [ArgActionMethod, ArgDescription("Pull remote changes from a remote repo")]
        public void Pull([ArgContextualAssistant(typeof(RemotePicker))][ArgRequired, ArgDescription("The name of the remote to pull from")]string remote, [DefaultValue("master"), PromptIfEmpty(HighlighterConfiguratorType = typeof(HashtagHighlighter)), ArgDescription("The name of the branch to pull")] string branch)
        {
            Console.WriteLine("Pulling from " + remote + ", branch=" + branch);
        }

        [ArgActionMethod, ArgDescription("Gets the status")]
        public void Status()
        {
            Console.WriteLine("Here is some status");
        }

        private class CustomHighlighter : IHighlighterConfigurator
        {
            public void Configure(SimpleSyntaxHighlighter highlighter)
            {
                highlighter.AddKeyword("master", ConsoleColor.Green);
            }
        }

        private class HashtagHighlighter : IHighlighterConfigurator
        {
            public void Configure(SimpleSyntaxHighlighter highlighter)
            {
                highlighter.AddRegex("#.*", ConsoleColor.Cyan);
            }
        }

        private class RemotePicker : ContextAssistSearch
        {
            public RemotePicker()
            {
              
            }

            protected override System.Collections.Generic.List<ContextAssistSearchResult> GetResults(string searchString)
            {
                var allRemotes = new List<string>
                {
                    "origin",
                    "upstream",
                    "dev",
                    "release",
                    "foo",
                    "bar"
                };

                return allRemotes.Where(r => r.StartsWith(searchString, StringComparison.InvariantCultureIgnoreCase))
                    .Select(r => ContextAssistSearchResult.FromString(r))
                    .ToList();
            }

            public override bool SupportsAsync
            {
                get { return false; }
            }

            protected override System.Threading.Tasks.Task<List<ContextAssistSearchResult>> GetResultsAsync(string searchString)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class CustomHighlighter : IHighlighterConfigurator
    {
        public void Configure(SimpleSyntaxHighlighter highlighter)
        {
            highlighter.AddKeyword("release", ConsoleColor.Red, comparison: StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public class Git
    {
        public static void _Main(string[] args)
        {
            Args.InvokeAction<GitExampleArgs>(args);
        }    
    }
}
