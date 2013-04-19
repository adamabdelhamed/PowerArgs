using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// An attribute used to define long form aliases for argument
    /// names.  For example, --log-level instead of -log.
    /// It also supports an alternate syntax for providing the values.
    /// For example: --log-level=error instead of -log error or /log:error.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple=true)]
    public class ArgLongForm : ArgHook
    {
        string value;
        PropertyInfo target;

        /// <summary>
        /// Creates a new ArgLongForm attirbute using the given long form string.
        /// </summary>
        /// <param name="value">The long form value.  You can provide the two dashes in this string or not.  The --long-form pattern will enforced at runtime either way.</param>
        public ArgLongForm(string value)
        {
            this.BeforeParsePriority = 100;
            if (value == null) throw new InvalidArgDefinitionException("Values for long form arguments cannot be null", new ArgumentNullException("value"));
            if (value.StartsWith("--"))
            {
                value = value.Substring(2);
            }

            this.value = value;

            var myUsageHook = new UsageHook();

            myUsageHook.HookExecuting += (usageInfo) =>
            {
                if (target == null)
                {
                    // This should ensure that the target should be populated if the current property is the target
                    try { Args.Parse(usageInfo.Property.PropertyType); } catch (Exception) { }
                }

                if (target == null) return;

                if (usageInfo.Property == target)
                {
                    usageInfo.Aliases.Add("--"+this.value);
                }
            };

            ArgUsage.RegisterHook(null, myUsageHook);
        }

        /// <summary>
        /// Finds instances of the long form alias on the command line
        /// and replaces it with a standard property specification.
        /// </summary>
        /// <param name="context">The context that has access to the command line args and the target property.</param>
        public override void BeforeParse(HookContext context)
        {
            this.target = context.Property;

            List<string> newCommandLine = new List<string>();
            foreach (var arg in context.CmdLineArgs)
            {
                if (arg.StartsWith("--"))
                {
                    var argumentName = arg.Substring(2);
                    if (argumentName.Contains("="))
                    {
                        if (argumentName.IndexOf('=') != argumentName.LastIndexOf('='))
                        {
                            throw new ArgumentException("The '=' character can only appear once in a long form argument");
                        }

                        var explicitValue = argumentName.Split('=')[1];
                        argumentName = argumentName.Split('=')[0];

                        TryReplaceArg(context, newCommandLine, arg, argumentName, explicitValue);
                    }
                    else
                    {
                        TryReplaceArg(context, newCommandLine, arg, argumentName, null);
                    }
                }
                else
                {
                    newCommandLine.Add(arg);
                }
            }

            context.CmdLineArgs = newCommandLine.ToArray();
        }

        private void TryReplaceArg(HookContext context, List<string> newCommandLine, string originalArg, string argumentName, string explicitValue)
        {
            bool ignoreCase = true;

            if (context.Property.HasAttr<ArgIgnoreCase>() &&
                context.Property.Attr<ArgIgnoreCase>().IgnoreCase == false)
            {
                ignoreCase = false;
            }

            if (ignoreCase && argumentName.ToLower() == this.value.ToLower())
            {
                newCommandLine.Add("-" + context.Property.GetArgumentName());
                if (explicitValue != null)
                {
                    newCommandLine.Add(explicitValue);
                }
            }
            else if (!ignoreCase && argumentName == this.value)
            {
                newCommandLine.Add("-" + context.Property.GetArgumentName());
                if (explicitValue != null)
                {
                    newCommandLine.Add(explicitValue);
                }
            }
            else
            {
                // No match - pass through
                newCommandLine.Add(originalArg);
            }
        }
    }
}
