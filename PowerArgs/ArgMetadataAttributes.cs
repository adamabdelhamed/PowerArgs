using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs
{
    public class ArgReviverAttribute : Attribute
    {
        public ArgReviverAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ArgActionType : Attribute
    {
        public Type ActionType { get; private set; }
        public ArgActionType(Type t)
        {
            this.ActionType = t;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : Attribute
    {
        public object Value { get; private set; }
        public DefaultValueAttribute(object value) { Value = value; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ArgIgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class ArgPosition : Attribute
    {
        public int Position { get; private set; }
        public ArgPosition(int pos)
        {
            this.Position = pos;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ArgDescription : Attribute
    {
        public string Description { get; private set; }
        public ArgDescription(string description)
        {
            this.Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple=true)]
    public class ArgExample : Attribute
    {
        public string Example { get; private set; }
        public string Description { get; private set; }
        public ArgExample(string example, string description)
        {
            this.Example = example;
            this.Description = description;
        }
    }

    public class ArgShortcut : Attribute
    {
        public string Shortcut { get; set; }

        public ArgShortcut(string shortcut)
        {
            this.Shortcut = shortcut;
        }
 
        public static string GetShortcut(PropertyInfo info)
        {
            var actionProperty = ArgAction.GetActionProperty(info.DeclaringType);
            if (actionProperty != null && actionProperty.Name == info.Name) return null;

            var attr = info.Attr<ArgShortcut>();

            if (attr == null) return info.Name.ToLower()[0]+"";
            else return attr.Shortcut;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class StickyArg : Attribute
    {
        private string file;
        private Dictionary<string, string> stickyArgs { get; set; }

        public StickyArg() : this(null) { }

        public StickyArg(string file)
        {
            stickyArgs = new Dictionary<string, string>();
            this.file = file ?? Assembly.GetEntryAssembly().Location + ".StickyArgs.txt";
            Load();
        }

        public string GetStickyArg(string name)
        {
            string ret = null;
            if (stickyArgs.TryGetValue(name, out ret) == false) return null;
            return ret;
        }

        public void SetStickyArg(string name, string value)
        {
            if (stickyArgs.ContainsKey(name))
            {
                stickyArgs[name] = value;
            }
            else
            {
                stickyArgs.Add(name, value);
            }
            Save();
        }

        private void Load()
        {
            stickyArgs.Clear();

            if (File.Exists(file) == false) return;

            foreach (var line in File.ReadAllLines(file))
            {
                int separator = line.IndexOf("=");
                if (separator < 0 || line.Trim().StartsWith("#")) continue;

                string key = line.Substring(0, separator).Trim();
                string val = separator == line.Length - 1 ? "" : line.Substring(separator + 1).Trim();

                stickyArgs.Add(key, val);
            }
        }

        private void Save()
        {
            var lines = (from k in stickyArgs.Keys select k + "=" + stickyArgs[k]).ToArray();
            File.WriteAllLines(file, lines);
        }
    }
}
