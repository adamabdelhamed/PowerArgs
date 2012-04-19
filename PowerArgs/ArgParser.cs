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

        protected Dictionary<string, string> Args { get; set; }

        public string this[string specifiedArg]
        {
            get
            {
                return Args[specifiedArg];
            }
        }

        public bool ContainsLeftOverArgs()
        {
            return Args.Keys.Count > 0;
        }

        public IEnumerable<string> SpecifiedArguments()
        {
            return Args.Keys;
        }

        public string GetAndRemoveArgValueText(PropertyInfo prop)
        {
            string matchedKey = null;
            foreach (var key in Args.Keys)
            {
                if (prop.MatchesSpecifiedArg(key))
                {
                    matchedKey = key;
                    break;
                }
            }

            if (matchedKey != null)
            {
                var val = Args[matchedKey];
                Args.Remove(matchedKey);
                return val;
            }
            else
            {
                return null;
            }
        }

        public ArgParser(Type argType, Dictionary<Type, Func<string, string, object>> revivers = null)
        {
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

    internal class SmartArgParser : ArgParser
    {
        public SmartArgParser(Type argType, Dictionary<Type, Func<string, string, object>> revivers = null) 
            : base(argType, revivers) { }

        protected override void ParseInternal(string[] args, PropertyInfo actionArgProp)
        {
            if (args.Length == 0) return;

            var positionParser = new PositionArgParser(argType, revivers);
            positionParser.Parse(args, actionArgProp);

            ArgParser inner = argType.GetArgStyle() == ArgStyle.SlashColon ? (ArgParser)new SlashColonParser(argType, revivers) : (ArgParser)new PowerShellStyleParser(argType, revivers);
            inner.Parse(args, actionArgProp);

            foreach (var key in positionParser.SpecifiedArguments())
            {
                Args.Add(key, positionParser[key]);
            }

            foreach (var key in inner.SpecifiedArguments())
            {
                Args.Add(key, inner[key]);
            }
        }


        private class PositionArgParser : ArgParser
        {
            public PositionArgParser(Type argType, Dictionary<Type, Func<string, string, object>> revivers = null) 
                : base(argType, revivers) { }

            protected override void ParseInternal(string[] args, PropertyInfo actionArgProp)
            {
                List<PropertyInfo> positionalArgs = FindAllPositionalPropertyies(argType, actionArgProp);
                for (int i = 0; i < args.Length; i++)
                {
                    if (argType.GetArgStyle() == ArgStyle.PowerShell && args[i].StartsWith("-")) break;
                    if (argType.GetArgStyle() == ArgStyle.SlashColon && args[i].StartsWith("/")) break;

                    var matchingProp = (from prop in positionalArgs where prop.Attr<ArgPosition>() != null && prop.Attr<ArgPosition>().Position == i select prop.Name).FirstOrDefault();
                    if (matchingProp == null) continue;

                    Args.Add(matchingProp, args[i]);
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
            public SlashColonParser(Type argType, Dictionary<Type, Func<string, string, object>> revivers = null) : base(argType, revivers) { }

            protected override void ParseInternal(string[] args, PropertyInfo actionArgProp)
            {
                var matches = (from a in args
                               where a.StartsWith("/")
                               select new
                               {
                                   Key = a.Contains(":") ? a.Substring(1, a.IndexOf(":") - 1).Trim() : a.Substring(1, a.Length - 1),
                                   Value = a.Contains(":") ? a.Substring(a.IndexOf(":") + 1).Trim() : ""
                               });

                var empties = from a in args where a == "/" select a;

                if (empties.Count() > 0) throw new ArgException("Argument name missing");

                foreach (var match in matches)
                {
                    Args.Add(match.Key, match.Value);
                }
            }
        }

        private class PowerShellStyleParser : ArgParser
        {
            public PowerShellStyleParser(Type argType, Dictionary<Type, Func<string, string, object>> revivers = null) : base(argType, revivers) { }

            protected override void ParseInternal(string[] args, PropertyInfo actionArgProp)
            {
                string currentArg = null;
                foreach (var a in args)
                {
                    //TODO - if the user enters an extra argument that does not start with a '-'
                    //       and also does not match a positional arg then an exception will not be
                    //       thrown even though there's an extra, unused arg
                    
                    if (currentArg == null && a.StartsWith("-") != true) continue;

                    if (currentArg == null)
                    {
                        currentArg = a.Substring(1);
                        if (currentArg.Length == 0) throw new ArgException("argument name missing");
                    }
                    else if (a.StartsWith("-") && a.Length > 1 && char.IsDigit(a[1]) == false)
                    {
                        Args.Add(currentArg, "");
                        currentArg = a.Substring(1);
                    }
                    else
                    {
                        Args.Add(currentArg, a);
                        currentArg = null;
                    }
                }

                if (currentArg != null) Args.Add(currentArg, "");
            }
        }
    }
}
