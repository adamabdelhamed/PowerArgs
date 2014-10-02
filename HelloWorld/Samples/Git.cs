using PowerArgs;
using System;

namespace HelloWorld.Samples
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling, ShowTypeColumn = false), ArgDescription("A sample that uses the familiar git command line tool to show how to implement programs with multiple actions."), TabCompletion /*[TabCompletion] is useful for the sample, but you don't need it in your program (unless you want it).*/ ]
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
        public void Pull([ArgRequired, ArgDescription("The name of the remote to pull from")]string remote, [DefaultValue("master"), ArgDescription("The name of the branch to pull")] string branch)
        {
            Console.WriteLine("Pulling from " + remote + ", branch=" + branch);
        }

        [ArgActionMethod, ArgDescription("Gets the status")]
        public void Status()
        {
            Console.WriteLine("Here is some status");
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
