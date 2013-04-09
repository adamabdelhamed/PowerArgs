using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace PowerArgs
{
    public static class ArgUsage
    {
        private static Dictionary<string, string> KnownTypeMappings = new Dictionary<string, string>()
        {
            {"Int32", "integer"},
            {"Int64", "integer"},
            {"Boolean", "switch"},
            {"Guid", "guid"},
        };

        private static string GetFriendlyTypeName(Type t)
        {
            var name = t.Name;
            if (KnownTypeMappings.ContainsKey(name))
            {
                return KnownTypeMappings[name];
            }
            else
            {
                return name.ToLower();
            }
        }

        public static string GetUsage<T>(string exeName = null)
        { 
            return GetStyledUsage<T>(exeName).ToString();
        }

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

        private static ConsoleString GetOptionsUsage(IEnumerable<PropertyInfo> options, bool ignoreActionProperties)
        {

            var hasPositionalArgs = options.Where(o => o.HasAttr<ArgPosition>()).Count() > 0;

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

            foreach (PropertyInfo prop in options.OrderBy(o => o.HasAttr<ArgPosition>() ? o.Attr<ArgPosition>().Position : 1000))
            {
                if (prop.HasAttr<ArgIgnoreAttribute>()) continue;
                if (prop.IsActionArgProperty() && ignoreActionProperties) continue;
                if (prop.Name == Constants.ActionPropertyConventionName && ignoreActionProperties) continue;

                var positionString = new ConsoleString(prop.HasAttr<ArgPosition>() ? prop.Attr<ArgPosition>().Position + "" : "NA");
                var requiredString = new ConsoleString(prop.HasAttr<ArgRequired>() ? "*" : "", ConsoleColor.Red);
                var descriptionString = new ConsoleString(prop.Attr<ArgDescription>() != null ? prop.Attr<ArgDescription>().Description : "");
                var typeString = new ConsoleString(GetFriendlyTypeName(prop.PropertyType));

                var indicator = "-";

                rows.Add(new List<ConsoleString>()
                {
                    new ConsoleString(indicator)+(prop.GetArgumentName() + " ("+ indicator + ArgShortcut.GetShortcut(prop) +")"),
                    typeString+requiredString,
                    descriptionString,
                });

                if (hasPositionalArgs)
                {
                    rows.Last().Insert(2, positionString);
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
