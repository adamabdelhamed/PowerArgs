using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    public abstract class ArgValidator : Attribute
    {
        public int Priority { get; set; }
        public abstract void Validate(string name, ref string arg);
    }

    public class ArgExistingFile : ArgValidator
    {
        public override void Validate(string name, ref string arg)
        {
            if (File.Exists(arg) == false)
            {
                throw new ArgException("File not found - " + arg, new FileNotFoundException());
            }
            arg = Path.GetFullPath(arg);
        }
    }

    public class ArgExistingDirectory : ArgValidator
    {

        public override void Validate(string name, ref string arg)
        {
            if (Directory.Exists(arg) == false)
            {
                throw new ArgException("Directory not found: '" + arg + "'", new DirectoryNotFoundException());
            }
            arg = Path.GetFullPath(arg);
        }
    }

    public class ArgRange : ArgValidator
    {
        double min, max;
        public ArgRange(double min, double max)
        {
            this.min = min;
            this.max = max;
        }

        public override void Validate(string name, ref string arg)
        {
            double d;
            if (double.TryParse(arg, out d) == false)
            {
                throw new ArgException("Expected a number for arg: " + name);
            }

            if (d < min || d > max)
            {
                throw new ArgException(name + " must be at least " + min + ", but not greater than " + max, new ArgumentOutOfRangeException());
            }
        }
    }

    public class ArgRequired : ArgValidator
    {
        public ArgRequired()
        {
            Priority = 100;
        }

        public bool PromptIfMissing { get; set; }

        public override void Validate(string name, ref string arg)
        {
            if (arg == null && PromptIfMissing)
            {
                var value = "";
                while (string.IsNullOrWhiteSpace(value))
                {
                    Console.Write("Enter value for " + name + ": ");
                    value = Console.ReadLine();
                }

                arg = value;
            }
            if (arg == null)
            {
                throw new ArgException("The argument '" + name + "' is required", new ArgumentNullException(name));
            }
        }
    }

    public class ArgRegex : ArgValidator
    {
        protected string regex;
        protected string errorMessage;

        protected Match exactMatch;
        public ArgRegex(string regex, string errorMessage = "Invalid argument")
        {
            this.regex = regex;
            this.errorMessage = errorMessage;
        }

        public override void Validate(string name, ref string arg)
        {
            string input = arg;
            MatchCollection matches = Regex.Matches(arg, regex);
            exactMatch = (from m in matches.ToList() where m.Value == input select m).SingleOrDefault();
            if (exactMatch == null) throw new ArgException(errorMessage+": " + arg);
        }
    }
}
