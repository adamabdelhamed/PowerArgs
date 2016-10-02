using Newtonsoft.Json;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class LevelDefinition : List<ThingDefinition>
    {
        public static readonly int Width = 60;
        public static readonly int Height = 10;

        public static string LevelBuilderLevelsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ConsoleZombies","LevelBuilder","Levels");
        
        public void Save(string levelName)
        {
            var fileName = System.IO.Path.Combine(LevelBuilderLevelsPath, levelName + ".json");

            if(System.IO.Directory.Exists(LevelBuilderLevelsPath) == false)
            {
                System.IO.Directory.CreateDirectory(LevelBuilderLevelsPath);
            }

            var defContents = JsonConvert.SerializeObject(this, Formatting.Indented);
            System.IO.File.WriteAllText(fileName, defContents);
        }

        public static List<string> GetLevelDefinitionFiles()
        {
            if (System.IO.Directory.Exists(LevelBuilderLevelsPath) == false)
            {
                return new List<string>();
            }
            return System.IO.Directory.GetFiles(LevelBuilderLevelsPath).Where(f => f.EndsWith(".json")).ToList();
        }

        public static LevelDefinition Load(string file)
        {
            var defContents = System.IO.File.ReadAllText(file);
            var def = JsonConvert.DeserializeObject<LevelDefinition>(defContents);
            return def;
        }

        public void Populate(Realm realm, bool builderMode)
        {
            foreach (var thingDef in this)
            {
                var thingType = Assembly.GetExecutingAssembly().GetType(thingDef.ThingType);
                var thing = Activator.CreateInstance(thingType) as PowerArgs.Cli.Physics.Thing;
                thing.Bounds = new PowerArgs.Cli.Physics.Rectangle(thingDef.InitialBounds.X, thingDef.InitialBounds.Y, thingDef.InitialBounds.W, thingDef.InitialBounds.H);

                
                if(thing is MainCharacter)
                {
                    (thing as MainCharacter).IsInLevelBuilder = builderMode;
                }
                else if(thing is Door)
                {
                    var closedRect = new Rectangle(
                        float.Parse(thingDef.InitialData["ClosedX"]),
                        float.Parse(thingDef.InitialData["ClosedY"]),
                        float.Parse(thingDef.InitialData["W"]),
                        float.Parse(thingDef.InitialData["H"]));

                    var openLocation = new Location(
                        float.Parse(thingDef.InitialData["OpenX"]),
                        float.Parse(thingDef.InitialData["OpenY"]));

                    (thing as Door).Initialize(closedRect, openLocation);
                    (thing as Door).IsOpen = true;
                }



                realm.Add(thing);
            }
        }
    }

    public class ThingDefinition
    {
        public string ThingType { get; set; }
        public Rectangle InitialBounds { get; set; }
        public Dictionary<string, string> InitialData { get; set; } = new Dictionary<string, string>();
    }
}
