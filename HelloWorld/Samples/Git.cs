using PowerArgs;
using System;

namespace HelloWorld.Samples
{
    [TabCompletion] // This is useful for the sample, but you don't need it in your program (unless you want it).
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling, ShowTypeColumn = false)]
    public class GitExampleArgs
    {
        [ArgActionMethod]
        [ArgDescription("Push your local changes to a remote repo")]
        public void Push(PushPullArgs args)
        {
            Console.WriteLine("Pushing to " + args.Remote);
        }

        [ArgActionMethod]
        [ArgDescription("Pull remote changes from a remote repo")]
        public void Pull(PushPullArgs args)
        {
            Console.WriteLine("Pulling from " + args.Remote);
        }

        [ArgActionMethod]
        [ArgDescription("Gets the status of the repo")]
        public void Status(StatusArgs args)
        {
            Console.WriteLine("Here is the status");
        }
    }

    public class PushPullArgs
    {
        [ArgPosition(1)]
        [ArgRequired]
        public string Remote { get; set; }

        [ArgPosition(2)]
        public string Branch { get; set; }
    }

    public class StatusArgs
    {
        [ArgShortcut("--long")]
        public bool Long { get; set; }

        [ArgShortcut("--short")]
        public bool Short { get; set; }
    }

    public class Git
    {
        public static void _Main(string[] args)
        {
            Args.InvokeAction<GitExampleArgs>(args);
        }
    }
}
