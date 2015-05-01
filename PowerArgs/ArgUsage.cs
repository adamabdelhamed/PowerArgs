﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// A class that lets you customize how your usage displays
    /// </summary>
    public class ArgUsageOptions
    {
        /// <summary>
        /// Set to true if you want to show the type column (true by default)
        /// </summary>
        public bool ShowType { get; set; }

        /// <summary>
        /// Set to true if you want to show the position column (true by default)
        /// </summary>
        public bool ShowPosition { get; set; }

        /// <summary>
        /// Set to true to list possible values (usually for enums, true by default)
        /// </summary>
        public bool ShowPossibleValues { get; set; }

        /// <summary>
        /// Set to true if you want to show default values after the description (true by default)
        /// </summary>
        public bool AppendDefaultValueToDescription { get; set; }

        /// <summary>
        /// Creates a new instance of ArgUsageOptions
        /// </summary>
        public ArgUsageOptions()
        {
            ShowType = true;
            ShowPosition = true;
            ShowPossibleValues = true;
            AppendDefaultValueToDescription = true;
        }
    }

    /// <summary>
    /// An attribute used to hook into the usage generation process and influence
    /// the content that is written.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public class UsageHook : Attribute, IGlobalArgMetadata
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
            if (HookExecuting != null) HookExecuting(info);
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
        /// Possible values for this option.  This is auto populated for enums and includes the description if specified.
        /// </summary>
        public List<string> PossibleValues { get; private set; }

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
        [Obsolete("Usage is not affected by this property")]
        public bool IsActionArgs { get; set; }

        /// <summary>
        /// The reflected property that this info object represents
        /// </summary>
        public PropertyInfo Property { get; set; }

        /// <summary>
        /// The command line argument that the system is currently generating usage for
        /// </summary>
        public CommandLineArgument Argument { get; set; }

        /// <summary>
        /// The default value for the argument
        /// </summary>
        public object DefaultValue { get; set; }

        private ArgumentUsageInfo()
        {
            Aliases = new List<string>();
            PossibleValues = new List<string>();
        }

        /// <summary>
        /// Generate a new info instance given a reflected property. 
        /// </summary>
        /// <param name="toAutoGen">The property to use to seed the usage info</param>
        public ArgumentUsageInfo(CommandLineArgument toAutoGen)
            : this()
        {
            this.Argument = toAutoGen;
            Property = toAutoGen.Source as PropertyInfo;
            Ignore = false;
            IsAction = toAutoGen.DefaultAlias == Constants.ActionPropertyConventionName;
            DefaultValue = toAutoGen.DefaultValue;
            IsRequired = toAutoGen.IsRequired;

            Name = "-" + toAutoGen.DefaultAlias;

            if (Name.EndsWith(Constants.ActionArgConventionSuffix))
            {
                Name = Name.Substring(0, Name.Length - Constants.ActionArgConventionSuffix.Length);
            }

            Aliases.AddRange(toAutoGen.Aliases.Skip(1).Select(a => "-" + a));

            Type = toAutoGen.ArgumentType.Name;
            if (KnownTypeMappings.ContainsKey(Type))
            {
                Type = KnownTypeMappings[Type];
            }
            else
            {
                Type = Type.ToLower();
            }

            Position = toAutoGen.Position >= 0 ? new int?(toAutoGen.Position) : null;
            Description = toAutoGen.Description ?? "";

            if (toAutoGen.ArgumentType.IsEnum)
            {
                foreach (var val in toAutoGen.ArgumentType.GetFields().Where(v => v.IsSpecialName == false))
                {
                    var description = val.HasAttr<ArgDescription>() ? " - " + val.Attr<ArgDescription>().Description : "";
                    var valText = val.Name;
                    PossibleValues.Add(valText + description);
                }
            }
        }
    }

    /// <summary>
    /// A helper class that generates usage documentation for your command line arguments given a custom argument
    /// scaffolding type.
    /// </summary>
    public static class ArgUsage
    {
        internal const int maxTableWidth = 79;
        internal static Dictionary<PropertyInfo, List<UsageHook>> ExplicitPropertyHooks = new Dictionary<PropertyInfo, List<UsageHook>>();
        internal static List<UsageHook> GlobalUsageHooks = new List<UsageHook>();

        // TODO P0 - Need unit tests for usage hooks

        /// <summary>
        /// Registers a usage hook for the given property.
        /// </summary>
        /// <param name="prop">The property to hook into or null to hook into all properties.</param>
        /// <param name="hook">The hook implementation.</param>
        [Obsolete("With the new CommandLineArgumentsDefinition model, you can add your usage hooks to the Metadata in the definition explicitly and then pass the definition to the GetUsage() methods.  Example: myDefinition.Metadata.Add(myHook);")]
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
        /// <param name="options">Specify custom usage options</param>
        /// <returns>the usage documentation as a string</returns>
        public static string GetUsage<T>(string exeName = null, ArgUsageOptions options = null)
        {
            return GetStyledUsage<T>(exeName, options).ToString();
        }

        /// <summary>
        /// Generates usage documentation for the given argument definition.
        /// </summary>
        /// <param name="definition">The definition of the command line arguments for a program</param>
        /// <param name="exeName">The name of your program or null if you want PowerArgs to automatically detect it.</param>
        /// <param name="options">Specify custom usage options</param>
        /// <returns>the usage documentation as a string</returns>
        public static string GetUsage(CommandLineArgumentsDefinition definition, string exeName = null, ArgUsageOptions options = null)
        {
            return GetStyledUsage(definition, exeName, options).ToString();
        }

        /// <summary>
        /// Generates color styled usage documentation for the given argument scaffold type.  
        /// </summary>
        /// <typeparam name="T">Your custom argument scaffold type</typeparam>
        /// <param name="exeName">The name of your program or null if you want PowerArgs to automatically detect it.</param>
        /// <param name="options">Specify custom usage options</param>
        /// <returns>the usage documentation as a styled string that can be printed to the console</returns>
        public static ConsoleString GetStyledUsage<T>(string exeName = null, ArgUsageOptions options = null)
        {
            return GetStyledUsage(typeof(T), exeName, options);
        }

        /// <summary>
        /// Generates color styled usage documentation for the given argument scaffold type.  
        /// </summary>
        /// <param name="t">Your custom argument scaffold type</param>
        /// <param name="exeName">The name of your program or null if you want PowerArgs to automatically detect it.</param>
        /// <param name="options">Specify custom usage options</param>
        /// <returns>the usage documentation as a styled string that can be printed to the console</returns>
        public static ConsoleString GetStyledUsage(Type t, string exeName = null, ArgUsageOptions options = null)
        {
            return GetStyledUsage(new CommandLineArgumentsDefinition(t), exeName, options);
        }

        /// <summary>
        /// Generates color styled usage documentation for the given arguments definition.  
        /// </summary>
        /// <param name="definition">The definition of the command line arguments for a program</param>
        /// <param name="exeName">The name of your program or null if you want PowerArgs to automatically detect it.</param>
        /// <param name="options">Specify custom usage options</param>
        /// <returns>the usage documentation as a styled string that can be printed to the console</returns>
        public static ConsoleString GetStyledUsage(CommandLineArgumentsDefinition definition, string exeName = null, ArgUsageOptions options = null)
        {
            options = options ?? new ArgUsageOptions();
            if (exeName == null)
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    throw new InvalidOperationException("PowerArgs could not determine the name of your executable automatically.  This may happen if you run GetUsage<T>() from within unit tests.  Use GetUsageT>(string exeName) in unit tests to avoid this exception.");
                }
                exeName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            }

            ConsoleString ret = new ConsoleString();

            ret += new ConsoleString("Usage: " + exeName, ConsoleColor.Cyan);


            if (definition.Actions.Count > 0)
            {
                ret.AppendUsingCurrentFormat(" <action> options\n\n");

                foreach (var example in definition.Examples)
                {
                    ret += new ConsoleString("EXAMPLE: " + example.Example + "\n" + example.Description + "\n\n", ConsoleColor.DarkGreen);
                }

                var global = GetOptionsUsage(definition.Arguments, true, options);

                if (string.IsNullOrEmpty(global.ToString()) == false)
                {
                    ret += new ConsoleString("Global options:\n\n", ConsoleColor.Cyan) + global + "\n";
                }

                ret += "Actions:";

                foreach (var action in definition.Actions)
                {
                    ret += "\n\n" + action.DefaultAlias + " - " + action.Description + "\n\n";

                    foreach (var example in action.Examples)
                    {
                        ret += new ConsoleString() + "   EXAMPLE: " + new ConsoleString(example.Example + "\n", ConsoleColor.Green) +
                            new ConsoleString("   " + example.Description + "\n\n", ConsoleColor.DarkGreen);
                    }

                    ret += GetOptionsUsage(action.Arguments, false, options);
                }
            }
            else
            {
                ret.AppendUsingCurrentFormat(" options\n\n");

                ret += GetOptionsUsage(definition.Arguments, false, options);

                ret += "\n";

                foreach (var example in definition.Examples)
                {
                    ret += new ConsoleString() + "   EXAMPLE: " + new ConsoleString(example.Example + "\n", ConsoleColor.Green);
                    var lines = WordWrapDescription(example.Description, 3, maxTableWidth);
                    foreach (string line in lines)
                    {
                        ret += new ConsoleString(line + "\n", ConsoleColor.DarkGreen);
                    }
                    ret += "\n";
                }
            }

            return ret;
        }

        private static string[] WordWrapDescription(string description, int indent, int width)
        {
            if ((description.Length + indent) <= width)
                return new string[] { new string(' ', indent) + description };

            List<string> lines = new List<string>(2);

            int start = 0;
            while (start < description.Length)
            {
                int pos = start + width - indent;
                if (pos >= description.Length)
                {
                    pos = description.Length - 1;
                }
                else
                {
                    // since I am not at the end, move backwards to the next word
                    while (pos > start && !char.IsWhiteSpace(description[pos])) pos--;
                    if (pos <= start)
                    {
                        // we couldn't find a space because we have a really long word
                        // so split back at the original position
                        pos = start + width - indent;
                    }
                }

                var line = new string('*', indent) + description.Substring(start, pos - start);
                lines.Add(line);

                // move the next work
                start = pos + 1;
                while (start < description.Length && char.IsWhiteSpace(description[start])) start++;
            }

            return lines.ToArray();
        }

        private static ConsoleString GetOptionsUsage(IEnumerable<CommandLineArgument> opts, bool ignoreActionProperties, ArgUsageOptions options)
        {
            var usageInfos = opts.Select(o => new ArgumentUsageInfo(o));

            var hasPositionalArgs = usageInfos.Where(i => i.Position >= 0).Count() > 0;

            List<ConsoleString> columnHeaders = new List<ConsoleString>()
            {
                new ConsoleString("OPTION", ConsoleColor.Yellow),
                new ConsoleString("DESCRIPTION", ConsoleColor.Yellow),
            };

            bool hasTypeCol = false, hasPosCol = false;

            int insertPosition = 1;
            if (options.ShowType)
            {
                columnHeaders.Insert(insertPosition++, new ConsoleString("TYPE", ConsoleColor.Yellow));
                hasTypeCol = true;
            }

            if (hasPositionalArgs && options.ShowPosition)
            {
                columnHeaders.Insert(insertPosition, new ConsoleString("POSITION", ConsoleColor.Yellow));
                hasPosCol = true;
            }

            List<List<ConsoleString>> rows = new List<List<ConsoleString>>();

            foreach (ArgumentUsageInfo usageInfo in usageInfos.OrderBy(i => i.Position >= 0 ? i.Position : 1000))
            {
                var hooks = new List<UsageHook>();
                if (usageInfo.Property != null && ArgUsage.ExplicitPropertyHooks.ContainsKey(usageInfo.Property))
                {
                    hooks.AddRange(ArgUsage.ExplicitPropertyHooks[usageInfo.Property]);
                }

                hooks.AddRange(ArgUsage.GlobalUsageHooks);

                hooks.AddRange(usageInfo.Argument.UsageHooks);

                foreach (var hook in hooks)
                {
                    hook.BeforeGenerateUsage(usageInfo);
                }


                if (usageInfo.Ignore) continue;
                if (usageInfo.IsAction && ignoreActionProperties) continue;

                var positionString = new ConsoleString(usageInfo.Position >= 0 ? usageInfo.Position + "" : "NA");
                var requiredString = new ConsoleString(usageInfo.IsRequired ? "*" : "", ConsoleColor.Red);
                var descriptionString = new ConsoleString(usageInfo.Description);
                if (options.AppendDefaultValueToDescription && usageInfo.DefaultValue != null) descriptionString += new ConsoleString(" [default=" + usageInfo.DefaultValue.ToString() + "]", ConsoleColor.DarkGreen);

                var typeString = new ConsoleString(usageInfo.Type);

                var aliases = usageInfo.Aliases.OrderBy(a => a.Length).ToList();
                var maxInlineAliasLength = 8;
                string inlineAliasInfo = "";

                int aliasIndex;
                for (aliasIndex = 0; aliasIndex < aliases.Count; aliasIndex++)
                {
                    var proposedInlineAliases = inlineAliasInfo == string.Empty ? aliases[aliasIndex] : inlineAliasInfo + ", " + aliases[aliasIndex];
                    if (proposedInlineAliases.Length <= maxInlineAliasLength)
                    {
                        inlineAliasInfo = proposedInlineAliases;
                    }
                    else
                    {
                        break;
                    }
                }

                if (inlineAliasInfo != string.Empty) inlineAliasInfo = " (" + inlineAliasInfo + ")";

                rows.Add(new List<ConsoleString>()
                {
                    new ConsoleString("")+(usageInfo.Name + inlineAliasInfo),
                    descriptionString,
                });

                insertPosition = 1;
                if (options.ShowType)
                {
                    rows.Last().Insert(insertPosition++, typeString + requiredString);
                }

                if (hasPositionalArgs && options.ShowPosition)
                {
                    rows.Last().Insert(insertPosition, positionString);
                }

                for (int i = aliasIndex; i < aliases.Count; i++)
                {
                    rows.Add(new List<ConsoleString>()
                    {
                        new ConsoleString("  "+aliases[i]),
                        ConsoleString.Empty,
                    });

                    if (hasTypeCol) rows.Last().Add(ConsoleString.Empty);
                    if (hasPosCol) rows.Last().Add(ConsoleString.Empty);
                }

                if (options.ShowPossibleValues)
                {
                    foreach (var possibleValue in usageInfo.PossibleValues)
                    {
                        rows.Add(new List<ConsoleString>()
                    {
                        ConsoleString.Empty,
                        new ConsoleString("  "+possibleValue),
                    });

                        if (hasTypeCol) rows.Last().Insert(rows.Last().Count - 1, ConsoleString.Empty);
                        if (hasPosCol) rows.Last().Insert(rows.Last().Count - 1, ConsoleString.Empty);
                    }
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
                int columnPosition = rowPrefix.Length;

                for (int i = 0; i < columns.Count; i++)
                {
                    var val = row[i];
                    while (val.Length < maximums[i] + buffer) val += " ";

                    // wrap the last column if it goes over
                    if (columnPosition + val.Length > maxTableWidth)
                    {
                        int limit = val.Length - 1;
                        if (Char.IsWhiteSpace(val[limit].Value))
                        {
                            // because we add spaces to the end
                            // we need to rewind
                            while (Char.IsWhiteSpace(val[limit].Value)) limit--;
                        }

                        int columnWidth = maxTableWidth - columnPosition;

                        // word-wrap the column string
                        int start = 0;
                        while (start < limit)
                        {
                            int pos = start + columnWidth;
                            if (pos > limit) pos = limit + 1;
                            if (start > 0) ret += "\n" + new string(' ', columnPosition);

                            // adjust the pos backwards to a space / wrap on a word
                            while (pos > start && !char.IsWhiteSpace(val[pos].Value)) pos--;
                            if (pos <= start)
                            {
                                // we couldn't find a space because we have a really long word
                                // so split back at the original position
                                pos = start + columnWidth;
                                if (pos > limit) pos = limit + 1;
                            }

                            ret += val.Substring(start, pos - start);

                            // adjust to the next block of string
                            start = pos + 1;
                            while (start < limit && char.IsWhiteSpace(val[start].Value)) start++;
                        }
                    }
                    else
                    {
                        ret += val;
                        columnPosition += val.Length;
                    }
                }
                ret += "\n";
            }

            return ret;
        }
    }
}
