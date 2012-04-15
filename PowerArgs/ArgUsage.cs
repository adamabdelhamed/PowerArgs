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

        public static string GetUsage<T>(string exeName = null)
        {
            return GetUsage<T>(ArgOptions.DefaultOptions, exeName);
        }

        public static string GetUsage<T>(ArgStyle style, string exeName = null)
        {
            ArgOptions options = ArgOptions.DefaultOptions;
            options.Style = style;
            return GetUsage<T>(options, exeName);
        }

        public static string GetUsage<T>(ArgOptions options, string exeName = null)
        {
            string ret = "Usage: ";
            ret += exeName ?? Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

            var actionProperty = ArgAction.GetActionProperty<T>();

            if (actionProperty != null)
            {
                ret += " <action> options\n\n";

                foreach (var example in typeof(T).Attrs<ArgExample>())
                {
                    ret += "EXAMPLE: " + example.Example + "\n" + example.Description + "\n\n";
                }

                var global = GetOptionsUsage(typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public), true, options);

                if (string.IsNullOrEmpty(global) == false)
                {
                    ret += "Global options:\n\n"+global+"\n";
                }

                ret += "Actions:";

                foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (prop.IsActionArgProperty() == false) continue;

                    var actionDescription = prop.HasAttr<ArgDescription>() ? " - " + prop.Attr<ArgDescription>().Description : "";

                    ret += "\n\n" + prop.GetArgumentName(options).Substring(0, prop.GetArgumentName(options).Length - ArgSettings.ActionArgConventionSuffix.Length) + actionDescription + "\n\n";

                    foreach (var example in prop.Attrs<ArgExample>())
                    {
                        ret += "   EXAMPLE: "+example.Example+"\n   " + example.Description+"\n\n";
                    }

                    ret += GetOptionsUsage(prop.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public), false, options);
                }
            }
            else
            {
                ret += " options\n\n";

                foreach (var example in typeof(T).Attrs<ArgExample>())
                {
                    ret += "EXAMPLE: " + example.Example + "\n" + example.Description + "\n\n";
                }

                ret += GetOptionsUsage(typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public), false, options);
            }
            
            return ret;
        }

        private static string GetOptionsUsage(IEnumerable<PropertyInfo> options, bool ignoreActionProperties, ArgOptions argOptions)
        {
            List<string> columnHeaders = new List<string>()
            {
                "Option",
                "Type",
                "Order",
                "Description"
            };

            List<List<string>> rows = new List<List<string>>();

            foreach (PropertyInfo prop in options.OrderBy(o => o.HasAttr<ArgPosition>() ? o.Attr<ArgPosition>().Position : 1000))
            {
                if (prop.HasAttr<ArgIgnoreAttribute>()) continue;
                if (prop.IsActionArgProperty() && ignoreActionProperties) continue;
                if (prop.Name == ArgSettings.ActionPropertyConventionName && ignoreActionProperties) continue;

                string positionString = prop.HasAttr<ArgPosition>() ? prop.Attr<ArgPosition>().Position + "" : "";
                string requiredString = prop.HasAttr<ArgRequired>() ? "*" : "";
                string descriptionString = prop.Attr<ArgDescription>() != null ? prop.Attr<ArgDescription>().Description : "";
                string typeString = prop.PropertyType.Name;

                if (typeString == "Boolean") typeString = "Switch";

                var indicator = argOptions.Style == ArgStyle.PowerShell ? "-" : "/";

                rows.Add(new List<string>()
                {
                    indicator+prop.GetArgumentName(argOptions) + " ("+ indicator + ArgShortcut.GetShortcut(prop, argOptions) +")",
                    typeString+requiredString,
                    positionString,
                    descriptionString
                });
            }

            return FormatAsTable(columnHeaders, rows, "   ");
        }

        private static string FormatAsTable(List<string> columns, List<List<string>> rows, string rowPrefix = "")
        {
            if (rows.Count == 0) return "";

            Dictionary<int, int> maximums = new Dictionary<int, int>();

            for (int i = 0; i < columns.Count; i++) maximums.Add(i, columns[i].Length);
            for (int i = 0; i < columns.Count; i++)
            {
                foreach (var row in rows)
                {
                    maximums[i] = Math.Max(maximums[i], row[i].Length);
                }
            }

            string ret = "";
            int buffer = 3;

            ret += rowPrefix;
            for (int i = 0; i < columns.Count; i++)
            {
                string val = columns[i].ToUpper();
                while (val.Length < maximums[i] + buffer) val += " ";
                ret += val;
            }

            ret += "\n";

            foreach (var row in rows)
            {
                ret += rowPrefix;
                for (int i = 0; i < columns.Count; i++)
                {
                    string val = row[i];
                    while (val.Length < maximums[i] + buffer) val += " ";

                    ret += val;
                }
                ret += "\n";
            }

            return ret;
        }
    }
}
