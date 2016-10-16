using Newtonsoft.Json;
using PowerArgs.Cli.Physics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConsoleZombies
{
    public class LevelDefinition
    {
        const string LevelFileExtension = ".czl";

        public static readonly int Width = 78;
        public static readonly int Height = 20;
        
        public static string LevelBuilderLevelsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LocalLevels");
        
        public List<ISerializableThing> Things { get; private set; } = new List<ISerializableThing>();

        private static JsonSerializerSettings SerializationSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
            Formatting = Formatting.Indented
        };

        public void Save(string levelName)
        {
            string fileName = levelName;
            if (Path.IsPathRooted(levelName) == false && File.Exists(levelName) == false)
            {
                fileName = System.IO.Path.Combine(LevelBuilderLevelsPath, levelName + ".czl");
            }

            var dir = Path.GetDirectoryName(fileName);
            if (Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }

            var defContents = JsonConvert.SerializeObject(this, SerializationSettings);
            System.IO.File.WriteAllText(fileName, defContents);
        }

        public static List<string> GetLevelDefinitionFiles()
        {
            if (System.IO.Directory.Exists(LevelBuilderLevelsPath) == false)
            {
                return new List<string>();
            }
            return System.IO.Directory.GetFiles(LevelBuilderLevelsPath).Where(f => f.EndsWith(LevelFileExtension)).ToList();
        }

        public static LevelDefinition Load(string file)
        {
            if (System.IO.File.Exists(file) == false)
            {
                file = System.IO.Path.Combine(LevelBuilderLevelsPath, file + LevelFileExtension);
            }

            var defContents = System.IO.File.ReadAllText(file);
            if (defContents == string.Empty)
            {
                return new LevelDefinition();
            }
            else
            {
                var def = JsonConvert.DeserializeObject<LevelDefinition>(defContents, SerializationSettings);
                return def;
            }
        }

        public void Hydrate(Scene scene, bool isInLevelBuilder)
        {
            foreach (var thingDef in Things.OrderBy(t => t.RehydrateOrderHint))
            {
                thingDef.Rehydrate(isInLevelBuilder);
                scene.Add(thingDef.HydratedThing);
            }
        }
    }
}
