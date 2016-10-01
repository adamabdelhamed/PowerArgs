using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    [TabCompletion]
    class Program
    {
        static void Main(string[] args)
        {
            Args.InvokeAction<Program>(args);
        }

        [ArgActionMethod]
        public void Play([ArgumentAwareTabCompletion(typeof(LevelCompletionType))]string levelFile)
        {
            if(levelFile.EndsWith(".json") == false)
            {
                var guessedFile = System.IO.Path.Combine(LevelDefinition.LevelBuilderLevelsPath,levelFile+".json");
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

            PrototypeLevel.Run(LevelDefinition.Load(levelFile));
        }

        [ArgActionMethod]
        public void Build()
        {
            new LevelBuilder().Run();
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
