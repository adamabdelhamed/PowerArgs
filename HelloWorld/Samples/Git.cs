using PowerArgs;
using System;

namespace HelloWorld.Samples
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling, ShowTypeColumn = false), TabCompletion /*[TabCompletion] is useful for the sample, but you don't need it in your program (unless you want it).*/ ]
    public class GitExampleArgs
    {
        [ArgActionMethod, ArgDescription("Push your local changes to a remote repo")]
        public void Push([ArgRequired]string remote, [DefaultValue("master")]string branch)
        {
            Console.WriteLine("Pushing to " + remote + ", branch=" + branch);
        }

        [ArgActionMethod, ArgDescription("Pull remote changes from a remote repo")]
        public void Pull([ArgRequired]string remote, [DefaultValue("master")] string branch)
        {
            Console.WriteLine("Pulling from " + remote + ", branch=" + branch);
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
