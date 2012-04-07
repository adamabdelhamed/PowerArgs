using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs
{
    public enum ArgStyle
    {
        PowerShell,  // named args are specified in the format "exeName -param1Name param1Value" 
        SlashColon   // named args are specified in the format "exeName -/param1Name:param1Value"
    }

    public abstract class ArgParser
    {
        protected Type argType;
        protected Dictionary<Type, Func<string, string, object>> revivers;
        protected ArgStyle style;

        public Dictionary<string, string> Args { get; private set; }

        public ArgParser(ArgStyle style,  Type argType, Dictionary<Type, Func<string, string, object>> revivers = null)
        {
            this.style = style;
            this.argType = argType;
            if (revivers == null) revivers = new Dictionary<Type, Func<string, string, object>>();
            this.revivers = revivers;
            Args = new Dictionary<string, string>();
        }

        protected abstract void ParseInternal(string[] args, PropertyInfo actionArgProp);

        public void Parse(string[] args, PropertyInfo actionArgProp)
        {
            ParseInternal(args, actionArgProp);
        }
    }

    public class SmartArgParser : ArgParser
    {
        public SmartArgParser(ArgStyle style, Type argType, Dictionary<Type, Func<string, string, object>> revivers = null) 
            : base(style, argType, revivers) { }

        protected override void ParseInternal(string[] args, PropertyInfo actionArgProp)
        {
            if (args.Length == 0) return;

            var positionParser = new PositionArgParser(style, argType, revivers);
            positionParser.Parse(args, actionArgProp);

            ArgParser inner = style == ArgStyle.PowerShell ? (ArgParser)new PowerShellStyleParser(argType, revivers) : (ArgParser)new SlashColonParser(argType, revivers);
            inner.Parse(args, actionArgProp);

            foreach (var key in positionParser.Args.Keys)
            {
                Args.Add(key.ToLower(), positionParser.Args[key]);
            }

            foreach (var key in inner.Args.Keys)
            {
                Args.Add(key.ToLower(), inner.Args[key]);
            }
        }


        private class PositionArgParser : ArgParser
        {
            public PositionArgParser(ArgStyle style, Type argType, Dictionary<Type, Func<string, string, object>> revivers = null) 
                : base(style, argType, revivers) { }

            protected override void ParseInternal(string[] args, PropertyInfo actionArgProp)
            {
                List<PropertyInfo> positionalArgs = FindAllPositionalPropertyies(argType, actionArgProp);
                for (int i = 0; i < args.Length; i++)
                {
                    if (style == ArgStyle.PowerShell && args[i].StartsWith("-")) break;
                    if (style == ArgStyle.SlashColon && args[i].StartsWith("/")) break;

                    var matchingProp = (from prop in positionalArgs where prop.Attr<ArgPosition>() != null && prop.Attr<ArgPosition>().Position == i select prop.Name).FirstOrDefault();
                    if (matchingProp == null) continue;

                    Args.Add(matchingProp.ToLower(), args[i]);
                }
            }

            private List<PropertyInfo> FindAllPositionalPropertyies(Type t, PropertyInfo actionArgProp)
            {
                List<PropertyInfo> ret = new List<PropertyInfo>();
                foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (prop.Attr<ArgPosition>() != null)
                    {
                        ret.Add(prop);
                    }
                }

                if (actionArgProp != null)
                {
                    ret.AddRange(FindAllPositionalPropertyies(actionArgProp.PropertyType, null));
                }

                return ret;
            }
        }

        private class SlashColonParser : ArgParser
        {
            public SlashColonParser(Type argType, Dictionary<Type, Func<string, string, object>> revivers = null) : base(ArgStyle.SlashColon, argType, revivers) { }

            protected override void ParseInternal(string[] args, PropertyInfo actionArgProp)
            {
                var matches = (from a in args
                               where a.ToLower().StartsWith("/")
                               select new
                               {
                                   Key = a.Contains(":") ? a.Substring(1, a.IndexOf(":") - 1).Trim() : a.Substring(1, a.Length - 1),
                                   Value = a.Contains(":") ? a.Substring(a.IndexOf(":") + 1).Trim() : ""
                               });

                foreach (var match in matches)
                {
                    Args.Add(match.Key, match.Value);
                }
            }
        }

        private class PowerShellStyleParser : ArgParser
        {
            public PowerShellStyleParser(Type argType, Dictionary<Type, Func<string, string, object>> revivers = null) : base(ArgStyle.PowerShell, argType, revivers) { }

            protected override void ParseInternal(string[] args, PropertyInfo actionArgProp)
            {
                string currentArg = null;
                foreach (var a in args)
                {
                    if (currentArg == null && a.StartsWith("-") != true) continue;

                    if (currentArg == null)
                    {
                        currentArg = a.Substring(1);
                    }
                    else if (a.StartsWith("-"))
                    {
                        Args.Add(currentArg.ToLower(), "");
                        currentArg = a.Substring(1);
                    }
                    else
                    {
                        Args.Add(currentArg.ToLower(), a);
                        currentArg = null;
                    }
                }

                if (currentArg != null) Args.Add(currentArg.ToLower(), "");
            }
        }
    }
}
