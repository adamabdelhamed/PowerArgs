using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PowerArgs
{
    public abstract class ArgValidator : Attribute
    {
        public abstract void Validate(string name, ref string arg);
    }

    public class ArgExistingFile : ArgValidator
    {

        public override void Validate(string name, ref string arg)
        {
            if (File.Exists(arg) == false)
            {
                throw new FileNotFoundException("File not found - " + arg);
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
                throw new DirectoryNotFoundException("Directory not found: '" + arg + "'");
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
                throw new ArgException(name + " must be at least " + min + ", but not greater than " + max);
            }
        }
    }

    public class ArgRequired : ArgValidator
    {
        public override void Validate(string name, ref string arg)
        {
            if (arg == null)
            {
                throw new Exception("The argument '" + name + "' is required");
            }
        }
    }
}
