using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Games
{
    public class Level
    {
        public const int DefaultWidth = 78;
        public const int DefaultHeight = 30;

        public string Name { get; set; }

        public int Width { get; set; } = DefaultHeight;
        public int Height { get; set; } = DefaultWidth;

        public List<LevelItem> Items { get; set; } = new List<LevelItem>();
      
    }

    public class LevelItem
    {
        public bool Ignore { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public char Symbol { get; set; }
        public ConsoleColor? FG { get; set; }
        public ConsoleColor? BG { get; set; }
        public List<String> Tags { get; set; } = new List<string>();

        public bool HasSimpleTag(string tag) => Tags.Where(t => t.ToLower().Equals(tag.ToLower())).Count() > 0;
        public bool HasValueTag(string tag) => Tags.Where(t => t.ToLower().StartsWith(tag.ToLower() + ":")).Count() > 0;

        public string GetTagValue(string key)
        {
            key = key.ToLower();
            if (TryGetTagValue(key, out string value) == false)
            {
                throw new ArgumentException("There is no value for key: "+key);
            }
            else
            {
                return value;
            }
        }

        public bool TryGetTagValue(string key, out string value)
        {
            key = key.ToLower();
            if(HasValueTag(key))
            {
                var tag = Tags.Where(t => t.ToLower().StartsWith(key + ":")).FirstOrDefault();
                value = ParseTagValue(tag);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        private string ParseTagValue(string tag)
        {
            var splitIndex = tag.IndexOf(':');
            if (splitIndex <= 0) throw new ArgumentException("No tag value present for tag: " + tag);

            var val = tag.Substring(splitIndex + 1, tag.Length - (splitIndex + 1));
            return val;
        }
    }
}
