using Newtonsoft.Json;
using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleGames
{
    public class Level
    {
        public List<LevelItem> Items { get; set; } = new List<LevelItem>();

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static Level Deserialize(string json) => JsonConvert.DeserializeObject<Level>(json);
    }

    public class LevelItem
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Symbol { get; set; }
        public ConsoleColor? FG { get; set; }
        public ConsoleColor? BG { get; set; }
        public List<String> Tags { get; set; } = new List<string>();
    }
}
