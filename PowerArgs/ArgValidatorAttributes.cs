using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// An abstract class that all validators should extend to validate user input from the command line.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ArgValidator : Attribute
    {
        /// <summary>
        /// Determines the order in which validators are executed.  Higher numbers execute first.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// If implemented in a derived class then ValidateAlways will be called for each property,
        /// even if that property wasn't specified by the user on the command line.  In this case the value
        /// will always be null.  This is useful for implementing validators such as [ArgRequired].
        /// 
        /// By default, the Validate(string,ref string) method is called unless a validator opts into ValidateAlways
        /// </summary>
        public virtual bool ImplementsValidateAlways { get { return false; } }

        /// <summary>
        /// Most validators should just override this method. It ONLY gets called if the user specified the 
        /// given argument on the command line, meaning you will never get a null for 'arg'.
        /// 
        /// If you want your validator to run even if the user did not specify the argument on the command line
        /// (for example if you were building something like [ArgRequired] then you should do 3 things.
        /// 
        /// 1 - Override the boolean ImplementsValidateAlways property so that it returns true
        /// 2 - Override the ValidateAlways() method instead
        /// 3 - Don't override the Validate() method since it will no longer be called
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="arg">The value specified on the command line.  If the user specified the property name, but not a value then arg will equal string.Empty.  The value will never be null.</param>
        public virtual void Validate(string name, ref string arg) { }

        /// <summary>
        /// Always validates the given property, even if it was not specified by the user (arg will be null in this case).
        /// If you override this method then you should also override ImplementsValidateAlways so it returns true.
        ///</summary>
        /// <param name="property">The property that the attribute was placed on.</param>
        /// <param name="arg">The value specified on the command line or null if the user didn't actually specify a value for the property.  If the user specified the property name, but not a value then arg will equal string.Empty</param>
        public virtual void ValidateAlways(PropertyInfo property, ref string arg) { throw new NotImplementedException(); }
    }

    /// <summary>
    /// Validates that if the user specifies a value for a property that the value represents a file that exists
    /// as determined by System.IO.File.Exists(file).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgExistingFile : ArgValidator
    {
        /// <summary>
        /// Validates that the given file exists and cleans up the argument so that the application has access
        /// to the full path.
        /// </summary>
        /// <param name="name">the name of the property being populated.  This validator doesn't do anything with it.</param>
        /// <param name="arg">The value specified on the command line</param>
        public override void Validate(string name, ref string arg)
        {
            if (File.Exists(arg) == false)
            {
                throw new ArgException("File not found - " + arg, new FileNotFoundException());
            }
            arg = Path.GetFullPath(arg);
        }
    }

    /// <summary>
    /// Validates that if the user specifies a value for a property that the value represents a directory that exists
    /// as determined by System.IO.Directory.Exists(directory).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgExistingDirectory : ArgValidator
    {

        /// <summary>
        /// Validates that the given directory exists and cleans up the argument so that the application has access
        /// to the full path.
        /// </summary>
        /// <param name="name">the name of the property being populated.  This validator doesn't do anything with it.</param>
        /// <param name="arg">The value specified on the command line</param>
        public override void Validate(string name, ref string arg)
        {
            if (Directory.Exists(arg) == false)
            {
                throw new ArgException("Directory not found: '" + arg + "'", new DirectoryNotFoundException());
            }
            arg = Path.GetFullPath(arg);
        }
    }

    /// <summary>
    /// Validates that the value is a number between the min and max (both inclusive) specified
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgRange : ArgValidator
    {
        double min, max;

        //TODO - Provide an option to make the max exclusive

        /// <summary>
        ///  Creates a new ArgRange validator.
        /// </summary>
        /// <param name="min">The minimum value (inclusive)</param>
        /// <param name="max">The maximum value (exclusive)</param>
        public ArgRange(double min, double max)
        {
            this.min = min;
            this.max = max;
        }

        /// <summary>
        /// Validates that the value is a number between the min and max (both inclusive) specifie
        /// </summary>
        /// <param name="name">the name of the property being populated.  This validator doesn't do anything with it.</param>
        /// <param name="arg">The value specified on the command line</param>
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

    /// <summary>
    /// Validates that the user actually provided a value for the given property on the command line.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgRequired : ArgValidator
    {
        /// <summary>
        /// Determines whether or not the validator should run even if the user doesn't specify a value on the command line.
        /// This value is always true for this validator.
        /// </summary>
        public override bool ImplementsValidateAlways
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Creates a new ArgRequired attribute.
        /// </summary>
        public ArgRequired()
        {
            Priority = 100;
        }

        /// <summary>
        /// If you set this to true and the user didn't specify a value then the command line will prompt the user for the value.
        /// </summary>
        public bool PromptIfMissing { get; set; }

        /// <summary>
        /// Validates that the user actually specified a value and optionally prompts them when it is missing.
        /// </summary>
        /// <param name="prop">The property being populated.  This validator doesn't do anything with it.</param>
        /// <param name="arg">The value specified on the command line or null if it wasn't specified</param>
        public override void ValidateAlways(PropertyInfo prop, ref string arg)
        {
            if (arg == null && PromptIfMissing)
            {
                var value = "";
                while (string.IsNullOrWhiteSpace(value))
                {
                    Console.Write("Enter value for " + prop.GetArgumentName() + ": ");
                    value = Console.ReadLine();
                }

                arg = value;
            }
            if (arg == null)
            {
                throw new ArgException("The argument '" + prop.GetArgumentName() + "' is required", new ArgumentNullException(prop.GetArgumentName()));
            }
        }
    }

    /// <summary>
    /// Performs regular expression validation on a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgRegex : ArgValidator
    {
        /// <summary>
        /// The regular expression to match
        /// </summary>
        protected string regex;

        /// <summary>
        /// A prefix for the error message to show in the case of a match.
        /// </summary>
        protected string errorMessage;

        /// <summary>
        /// The exact match that was found.
        /// </summary>
        protected Match exactMatch;

        /// <summary>
        /// Creates a new ArgRegex validator.
        /// </summary>
        /// <param name="regex">The regular expression that requires an exact match to be valid</param>
        /// <param name="errorMessage">A prefix for the error message to show in the case of a match.</param>
        public ArgRegex(string regex, string errorMessage = "Invalid argument")
        {
            this.regex = regex;
            this.errorMessage = errorMessage;
        }

        /// <summary>
        /// Validates that the given arg exactly matches the regular expression provided.
        /// </summary>
        /// <param name="name">the name of the property being populated.  This validator doesn't do anything with it.</param>
        /// <param name="arg">The value specified on the command line.</param>
        public override void Validate(string name, ref string arg)
        {
            string input = arg;
            MatchCollection matches = Regex.Matches(arg, regex);
            exactMatch = (from m in matches.ToList() where m.Value == input select m).SingleOrDefault();
            if (exactMatch == null) throw new ArgException(errorMessage+": " + arg);
        }
    }
}
