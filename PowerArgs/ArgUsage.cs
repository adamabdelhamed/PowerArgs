using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// An attribute used to hook into the usage generation process and influence
    /// the content that is written.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple=true)]
    public class UsageHook : Attribute
    {
        /// <summary>
        /// An event you can subscribe to in the case where you created
        /// your hook in running code rather than as a declarative attribute.
        /// </summary>
        public event Action<ArgumentUsageInfo> HookExecuting;

        /// <summary>
        /// This hook gets called when the property it is attached to is having
        /// its usage generated.  You can override this method and manipulate the
        /// properties of the given usage info object.
        /// </summary>
        /// <param name="info">An object that you can use to manipulate the usage output.</param>
        public virtual void BeforeGenerateUsage(ArgumentUsageInfo info)
        {
            if(HookExecuting != null) HookExecuting(info);
        }
    }

    /// <summary>
    /// A class that represents usage info to be written to the console.
    /// </summary>
    public class ArgumentUsageInfo
    {
        private static Dictionary<string, string> KnownTypeMappings = new Dictionary<string, string>()
        {
            {"Int32", "integer"},
            {"Int64", "integer"},
            {"Boolean", "switch"},
            {"Guid", "guid"},
        };

        /// <summary>
        /// The name that will be written as part of the usage.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Aliases for this argument that will be honored by the parser.  This
        /// includes shortcuts and long form aliases, but can be extended further.
        /// </summary>
        public List<string> Aliases { get; private set; }

        /// <summary>
        /// Indicates that the argument is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// The friendly type name that will be displayed to the user.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The expected position of the argument, or null if not a positioning is not supported for the given argument.
        /// </summary>
        public int? Position { get; set; }

        /// <summary>
        /// The description that will be written as part of the usage.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// If set to true, the argument usage will not be written.
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// True if this is the "Action" property
        /// </summary>
        public bool IsAction { get; set; }

        /// <summary>
        /// True if this represents a nested action argument property
        /// </summary>
        public bool IsActionArgs { get; set; }

        /// <summary>
        /// The reflected property that this info object represents
        /// </summary>
        public PropertyInfo Property { get; set; }

        private ArgumentUsageInfo()
        {
            Aliases = new List<string>();
        }

        /// <summary>
        /// Generate a new info instance given a reflected property. 
        /// </summary>
        /// <param name="toAutoGen">The property to use to seed the usage info</param>
        public ArgumentUsageInfo(PropertyInfo toAutoGen)
            : this()
        {
            Property = toAutoGen;
            Ignore = toAutoGen.HasAttr<ArgIgnoreAttribute>();
            IsAction = toAutoGen.IsActionArgProperty();
            IsActionArgs = toAutoGen.Name == Constants.ActionPropertyConventionName;

            Name = toAutoGen.GetArgumentName();
            IsRequired = toAutoGen.HasAttr<ArgRequired>();
            if (ArgShortcut.GetShortcut(toAutoGen) != null)
            {
                Aliases.Add("-" + ArgShortcut.GetShortcut(toAutoGen));
            }

            Type = toAutoGen.PropertyType.Name;
            if (KnownTypeMappings.ContainsKey(Type))
            {
                Type = KnownTypeMappings[Type];
            }
            else
            {
                Type = Type.ToLower();
            }

            Position = toAutoGen.HasAttr<ArgPosition>() ? new int?(toAutoGen.Attr<ArgPosition>().Position) : null;

            Description = "";

            if (toAutoGen.HasAttr<ArgDescription>())
            {
                Description = toAutoGen.Attr<ArgDescription>().Description;
            }
        }
    }

    /// <summary>
    /// A helper class that generates usage documentation for your command line arguments given a custom argument
    /// scaffolding type.
    /// </summary>
    public static class ArgUsage
    {
        internal static Dictionary<PropertyInfo, List<UsageHook>> ExplicitPropertyHooks = new Dictionary<PropertyInfo,List<UsageHook>>();
        internal static List<UsageHook> GlobalUsageHooks = new List<UsageHook>();

        /// <summary>
        /// Registers a usage hook for the given property.
        /// </summary>
        /// <param name="prop">The property to hook into or null to hook into all properties.</param>
        /// <param name="hook">The hook implementation.</param>
        public static void RegisterHook(PropertyInfo prop, UsageHook hook)
        {
            if (prop == null)
            {
                if (GlobalUsageHooks.Contains(hook) == false)
                {
                    GlobalUsageHooks.Add(hook);
                }
            }
            else
            {
                List<UsageHook> hookCollection;

                if (ExplicitPropertyHooks.TryGetValue(prop, out hookCollection) == false)
                {
                    hookCollection = new List<UsageHook>();
                    ExplicitPropertyHooks.Add(prop, hookCollection);
                }

                if (hookCollection.Contains(hook) == false)
                {
                    hookCollection.Add(hook);
                }
            }
        }

        /// <summary>
        /// Generates usage documentation for the given argument scaffold type.
        /// </summary>
        /// <typeparam name="T">Your custom argument scaffold type</typeparam>
        /// <param name="exeName">The name of your program or null if you want PowerArgs to automatically detect it.</param>
        /// <returns></returns>
        public static string GetUsage<T>(string exeName = null)
        { 
            return GetStyledUsage<T>(exeName).ToString();
        }

        /// <summary>
        /// Generates color styled usage documentation for the given argument scaffold type.  
        /// </summary>
        /// <typeparam name="T">Your custom argument scaffold type</typeparam>
        /// <param name="exeName">The name of your program or null if you want PowerArgs to automatically detect it.</param>
        /// <returns></returns>
        public static ConsoleString GetStyledUsage<T>(string exeName = null)
        {
            if (exeName == null)
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    throw new ArgException("PowerArgs could not determine the name of your executable automatically.  This may happen if you run GetUsage<T>() from within unit tests.  Use GetUsageT>(string exeName) in unit tests to avoid this exception.");
                }
                exeName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            }

            ConsoleString ret = new ConsoleString();

            ret += new ConsoleString("Usage: " + exeName, ConsoleColor.Cyan);

            var actionProperty = ArgAction.GetActionProperty<T>();

            if (actionProperty != null)
            {
                ret.AppendUsingCurrentFormat(" <action> options\n\n");

                foreach (var example in typeof(T).Attrs<ArgExample>())
                {
                    ret += new ConsoleString("EXAMPLE: " + example.Example + "\n" + example.Description + "\n\n", ConsoleColor.DarkGreen);
                }

                var global = GetOptionsUsage(typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public), true);

                if (string.IsNullOrEmpty(global.ToString()) == false)
                {
                    ret += new ConsoleString("Global options:\n\n", ConsoleColor.Cyan) + global + "\n";
                }

                ret += "Actions:";

                foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (prop.IsActionArgProperty() == false) continue;

                    var actionDescription = prop.HasAttr<ArgDescription>() ? " - " + prop.Attr<ArgDescription>().Description : "";

                    ret += "\n\n" + prop.GetArgumentName().Substring(0, prop.GetArgumentName().Length - Constants.ActionArgConventionSuffix.Length) + actionDescription + "\n\n";

                    foreach (var example in prop.Attrs<ArgExample>())
                    {
                        ret += new ConsoleString() + "   EXAMPLE: " + new ConsoleString(example.Example + "\n", ConsoleColor.Green) +
                            new ConsoleString("   " + example.Description + "\n\n", ConsoleColor.DarkGreen);
                    }

                    ret += GetOptionsUsage(prop.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public), false);
                }
            }
            else
            {
                ret.AppendUsingCurrentFormat(" options\n\n");

                ret += GetOptionsUsage(typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public), false);

                ret += "\n";

                foreach (var example in typeof(T).Attrs<ArgExample>())
                {
                    ret += new ConsoleString() + "   EXAMPLE: " + new ConsoleString(example.Example + "\n" , ConsoleColor.Green) + 
                        new ConsoleString("   "+example.Description + "\n\n", ConsoleColor.DarkGreen);
                }
            }
            
            return ret;
        }

        private static ConsoleString GetOptionsUsage(IEnumerable<PropertyInfo> opts, bool ignoreActionProperties)
        {
            var usageInfos = opts.Select(o => new ArgumentUsageInfo(o));

            var hasPositionalArgs = usageInfos.Where(i => i.Position >= 0).Count() > 0;

            List<ConsoleString> columnHeaders = new List<ConsoleString>()
            {
                new ConsoleString("OPTION", ConsoleColor.Yellow),
                new ConsoleString("TYPE", ConsoleColor.Yellow),
                new ConsoleString("DESCRIPTION", ConsoleColor.Yellow),
            };

            if (hasPositionalArgs)
            {
                columnHeaders.Insert(2, new ConsoleString("POSITION", ConsoleColor.Yellow));  
            }

            List<List<ConsoleString>> rows = new List<List<ConsoleString>>();

            foreach (ArgumentUsageInfo usageInfo in usageInfos.OrderBy(i => i.Position >= 0 ? i.Position : 1000))
            {
                foreach (var hook in usageInfo.Property.GetUsageHooks())
                {
                    hook.BeforeGenerateUsage(usageInfo);
                }

                if (usageInfo.Ignore) continue;
                if (usageInfo.IsAction && ignoreActionProperties) continue;
                if (usageInfo.IsActionArgs && ignoreActionProperties) continue;

                var positionString = new ConsoleString(usageInfo.Position >= 0 ? usageInfo.Position + "" : "NA");
                var requiredString = new ConsoleString(usageInfo.IsRequired ? "*" : "", ConsoleColor.Red);
                var descriptionString = new ConsoleString(usageInfo.Description);
                var typeString = new ConsoleString(usageInfo.Type);

                var indicator = "-";

                rows.Add(new List<ConsoleString>()
                {
                    new ConsoleString(indicator)+(usageInfo.Name + (usageInfo.Aliases.Count > 0 ? " ("+ usageInfo.Aliases[0] +")" : "")),
                    typeString+requiredString,
                    descriptionString,
                });

                if (hasPositionalArgs) rows.Last().Insert(2, positionString);

                for (int i = 1; i < usageInfo.Aliases.Count; i++)
                {
                    rows.Add(new List<ConsoleString>()
                    {
                        new ConsoleString("    "+usageInfo.Aliases[i]),
                        ConsoleString.Empty,
                        ConsoleString.Empty,
                    });

                    if (hasPositionalArgs) rows.Last().Insert(2, positionString);
                }

       
            }

            return FormatAsTable(columnHeaders, rows, "   ");
        }

        private static ConsoleString FormatAsTable(List<ConsoleString> columns, List<List<ConsoleString>> rows, string rowPrefix = "")
        {
            if (rows.Count == 0) return new ConsoleString();

            Dictionary<int, int> maximums = new Dictionary<int, int>();

            for (int i = 0; i < columns.Count; i++) maximums.Add(i, columns[i].Length);
            for (int i = 0; i < columns.Count; i++)
            {
                foreach (var row in rows)
                {
                    maximums[i] = Math.Max(maximums[i], row[i].Length);
                }
            }

            ConsoleString ret = new ConsoleString();
            int buffer = 3;

            ret += rowPrefix;
            for (int i = 0; i < columns.Count; i++)
            {
                var val = columns[i];
                while (val.Length < maximums[i] + buffer) val += " ";
                ret += val;
            }

            ret += "\n";

            foreach (var row in rows)
            {
                ret += rowPrefix;
                for (int i = 0; i < columns.Count; i++)
                {
                    var val = row[i];
                    while (val.Length < maximums[i] + buffer) val += " ";

                    ret += val;
                }
                ret += "\n";
            }

            return ret;
        }
    }
}
