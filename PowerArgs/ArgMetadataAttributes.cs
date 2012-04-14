using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs
{
    [AttributeUsage(AttributeTargets.Method)]
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

    #region Usage

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

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
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

    #endregion

    #region Hooks

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public abstract class ArgHook : Attribute
    {
        public class HookContext
        {
            public PropertyInfo Property { get; set; }
            public string ArgumentValue;
            public object Args { get; set; }
            public object RevivedProperty;
            public ArgParser Parser { get; set; }
            public ArgOptions Options { get; set; }
        }

        // Higher goes first
        public int BeforePopulatePropertyPriority { get; set; }
        public int AfterPopulatePropertyPriority { get; set; }
        public int BeforePopulatePropertiesPriority { get; set; }
        public int AfterPopulatePropertiesPriority { get; set; }

        public virtual void BeforePopulateProperty(HookContext context) { }
        public virtual void AfterPopulateProperty(HookContext context) { }
        public virtual void BeforePopulateProperties(HookContext context) { }
        public virtual void AfterPopulateProperties(HookContext context) { }
    }
    public class ArgShortcut : ArgHook
    {
        public string Shortcut { get; set; }

        public ArgShortcut(string shortcut)
        {
            this.Shortcut = shortcut;
            BeforePopulatePropertyPriority = 20;
        }

        public static string GetShortcut(PropertyInfo info, ArgOptions options = null)
        {
            options = options ?? ArgOptions.DefaultOptions;
            var actionProperty = ArgAction.GetActionProperty(info.DeclaringType);
            if (actionProperty != null && actionProperty.Name == info.Name) return null;

            var attr = info.Attr<ArgShortcut>();

            if (attr == null) return info.GetArgumentName(options)[0] + "";
            else return attr.Shortcut;
        }

        public override void BeforePopulateProperty(HookContext Context)
        {
            var argShortcut = GetShortcut(Context.Property);
            if (Context.ArgumentValue == null && argShortcut != null)
            {
                Context.ArgumentValue = Context.Parser.Args.ContainsKey(argShortcut) ? Context.Parser.Args[argShortcut] : null;
            }
        }

        public override void AfterPopulateProperty(HookContext Context)
        {
            var argShortcut = GetShortcut(Context.Property);
            if (argShortcut != null) Context.Parser.Args.Remove(argShortcut);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class StickyArg : ArgHook
    {
        private string file;
        private Dictionary<string, string> stickyArgs { get; set; }

        public StickyArg() : this(null) { }

        public StickyArg(string file)
        {
            stickyArgs = new Dictionary<string, string>();
            this.file = file ?? Assembly.GetEntryAssembly().Location + ".StickyArgs.txt";
            Load();
            BeforePopulatePropertyPriority = 10;
        }

        public override void BeforePopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue == null) Context.ArgumentValue = GetStickyArg(Context.Property.GetArgumentName(Context.Options));
        }

        public override void AfterPopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue != null) SetStickyArg(Context.Property.GetArgumentName(Context.Options), Context.ArgumentValue);
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

    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : ArgHook
    {
        public object Value { get; private set; }
        public DefaultValueAttribute(object value)
        {
            Value = value;
        }

        public override void BeforePopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue == null) Context.ArgumentValue = Value.ToString();
        }
    }

    // Internal - Do not use as an attribute
    internal class ParserCleanupHook : ArgHook
    {
        public ParserCleanupHook()
        {
            AfterPopulatePropertyPriority = -10;
            AfterPopulatePropertiesPriority = -10;
        }

        public override void AfterPopulateProperty(ArgHook.HookContext context)
        {
            context.Parser.Args.Remove(context.Property.GetArgumentName(context.Options));
        }

        public override void AfterPopulateProperties(ArgHook.HookContext context)
        {
            if (context.Parser.Args.Keys.Count > 0)
            {
                throw new ArgException("Unexpected argument '" + context.Parser.Args.Keys.First() + "'");
            }
        }
    } 

    #endregion
}
