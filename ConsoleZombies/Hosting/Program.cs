using PowerArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    [TabCompletion(HistoryToSave =100)][ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 1 && args[0].EndsWith(".czl"))
            {
                //Debugger.Launch();
                args = new string[] { "build", args[0] };
            }
            Args.InvokeAction<Program>(args);
        }

        [ArgActionMethod]
        public void Play([ArgumentAwareTabCompletion(typeof(LevelCompletionType))]string levelFile)
        {
            if(levelFile.EndsWith(".czl") == false)
            {
                var guessedFile = System.IO.Path.Combine(LevelDefinition.LevelBuilderLevelsPath,levelFile+".czl");
                if(System.IO.File.Exists(guessedFile) == false)
                {
                    throw new ArgException("No level called "+levelFile);
                }
                else
                {
                    levelFile = guessedFile;
                }
            }
            else if(System.IO.File.Exists(levelFile))
            {
                throw new ArgException("No level called " + levelFile);
            }

            var game = new GameApp();
            game.Load(LevelDefinition.Load(levelFile));
            game.Start().Wait();
        }

        [ArgActionMethod]
        public void Build(string levelId)
        {
            new LevelBuilder() { LevelId = levelId }.Start().Wait();
        }
    }

    public class LevelCompletionType : ISmartTabCompletionSource
    {
        SimpleTabCompletionSource innerSource;
        public LevelCompletionType() : base()
        {
            innerSource = new SimpleTabCompletionSource(LevelDefinition.GetLevelDefinitionFiles().Select(l => System.IO.Path.GetFileNameWithoutExtension(l).Contains(" ") ? "\""+System.IO.Path.GetFileNameWithoutExtension(l)+"\"" : System.IO.Path.GetFileNameWithoutExtension(l)));
        }

        public bool TryComplete(TabCompletionContext context, out string completion)
        {
            return innerSource.TryComplete(context, out completion);
        }
    }
}
