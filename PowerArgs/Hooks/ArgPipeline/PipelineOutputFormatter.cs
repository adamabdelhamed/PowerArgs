using PowerArgs.Preview;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PowerArgs
{
    /// <summary>
    ///  An interface that lets you define how certain objects should be formatted as ConsoleStrings
    /// </summary>
    public interface IPipelineOutputFormatter
    {
        /// <summary>
        /// Formats the given object as a ConsoleString
        /// </summary>
        /// <param name="o">the object to format</param>
        /// <returns>The formatted string</returns>
        ConsoleString Format(object o);
    }

    /// <summary>
    /// An attribute that can be used to add custom output formatting for a specific type to your application.  The formatter is used by the pipeline feature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class PipelineOutputFormatterAttribute : ArgHook
    {
        Type formatterType;
        Type targetType;

        IPipelineOutputFormatter formatter;

        /// <summary>
        /// Gets a reference to the formatter
        /// </summary>
        public IPipelineOutputFormatter Formatter
        {
            get
            {
                formatter = formatter ?? (IPipelineOutputFormatter)Activator.CreateInstance(formatterType);
                return formatter;
            }
        }

        /// <summary>
        /// Creates a new formatter for the given target and formatter types
        /// </summary>
        /// <param name="targetType">The type of object that this formatter is able to format</param>
        /// <param name="formatterType">The formatter type that must implement IPipelineOutputFormatter and have a default constructor</param>
        public PipelineOutputFormatterAttribute(Type targetType, Type formatterType)
        {
            if(formatterType.GetInterfaces().Contains(typeof(IPipelineOutputFormatter)) == false)
            {
                throw new InvalidArgDefinitionException("The type '" + formatterType + "' does not implement " + typeof(IPipelineOutputFormatter).FullName);
            }
            this.targetType = targetType;
            this.formatterType = formatterType;
        }

        /// <summary>
        /// Registers the formatter
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void BeforeInvoke(ArgHook.HookContext context)
        {
            PipelineOutputFormatter.RegisterFormatter(targetType, Formatter, true);
        }

        /// <summary>
        /// Unregisters the formatter
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void AfterInvoke(ArgHook.HookContext context)
        {
            if(PipelineOutputFormatter.GetFormatter(targetType) == Formatter)
            {
                PipelineOutputFormatter.UnregisterFormatter(targetType);
            }
        }
    }
    
    /// <summary>
    /// A class that lets you define a formatter from a Func.
    /// </summary>
    public class FuncPipelineOutputFormatter : IPipelineOutputFormatter
    {
        Func<object, ConsoleString> impl;

        private FuncPipelineOutputFormatter(Func<object, ConsoleString> impl)
        {
            this.impl = impl;
        }

        /// <summary>
        /// Creates a formatter from the given implementation
        /// </summary>
        /// <param name="impl">The function that implements formatting</param>
        public static IPipelineOutputFormatter Create(Func<object, ConsoleString> impl)
        {
            return new FuncPipelineOutputFormatter(impl);
        }

        /// <summary>
        /// Uses the function formatter implementation to format the given object
        /// </summary>
        /// <param name="o">The object to format</param>
        /// <returns>The formatted string</returns>
        public ConsoleString Format(object o)
        {
            return impl(o);
        }
    }

    /// <summary>
    /// a static class that can be used to format objects as ConsoleStrings
    /// </summary>
    public static class PipelineOutputFormatter
    {
        private static Dictionary<Type, IPipelineOutputFormatter> Formatters = CreateBuiltInFormatters();

        private static Dictionary<Type, IPipelineOutputFormatter> CreateBuiltInFormatters()
        {
            Dictionary<Type, IPipelineOutputFormatter> ret = new Dictionary<Type, IPipelineOutputFormatter>();

            var toStringFormatter = FuncPipelineOutputFormatter.Create((o) => { return new ConsoleString("" + o); });
            ret.Add(typeof(int), toStringFormatter);
            ret.Add(typeof(string), toStringFormatter);
            ret.Add(typeof(double), toStringFormatter);
            ret.Add(typeof(bool), toStringFormatter);
            ret.Add(typeof(char), toStringFormatter);
            ret.Add(typeof(Guid), toStringFormatter);
            ret.Add(typeof(DateTime), toStringFormatter);
            ret.Add(typeof(DateTimeOffset), toStringFormatter);
            ret.Add(typeof(float), toStringFormatter);
            ret.Add(typeof(byte), toStringFormatter);
            ret.Add(typeof(short), toStringFormatter);
            ret.Add(typeof(long), toStringFormatter);
            ret.Add(typeof(Uri), toStringFormatter);
            ret.Add(typeof(IPAddress), toStringFormatter);

            ret.Add(typeof(ConsoleString), FuncPipelineOutputFormatter.Create((str) => { return (ConsoleString)str; }));
            ret.Add(typeof(ConsoleCharacter), FuncPipelineOutputFormatter.Create((c) => { return new ConsoleString(new ConsoleCharacter[] { (ConsoleCharacter)c }); }));

            return ret;
        }

        private static IPipelineOutputFormatter DefaultFormatter = FuncPipelineOutputFormatter.Create((o) =>
        {
            if (o is IEnumerable)
            {
                Table t = new Table(new string[0]);
                foreach(var item in (IEnumerable)o)
                {
                    t.ExplicitAdd(item);
                }
                return t.CreateTable();
            }
            else
            {
                ConsoleTableBuilder builder = new ConsoleTableBuilder();
                List<ConsoleString> headers = new List<ConsoleString>() { new ConsoleString("PROPERTY", ConsoleColor.Yellow), new ConsoleString("VALUE", ConsoleColor.Yellow) };
                List<List<ConsoleString>> rows = new List<List<ConsoleString>>();
                foreach (var property in o.GetType().GetProperties())
                {
                    rows.Add(new List<ConsoleString>() { new ConsoleString(property.Name, ConsoleColor.Gray), new ConsoleString("" + property.GetValue(o, null), ConsoleColor.Green) });
                }

                var ret = builder.FormatAsTable(headers, rows);
                ret = new ConsoleString("Pipeline output of type: " + o.GetType().FullName + "\n") + ret;
                return ret;
            }
        });

        /// <summary>
        /// Formats the given object into a ConsoleString.  If a registered formatter matches the object's type then it is used,
        /// otherwise the default formatter is used.
        /// </summary>
        /// <param name="o">The object to format</param>
        /// <returns>The formatted string</returns>
        public static ConsoleString Format(object o)
        {
            IPipelineOutputFormatter formatter;
            if(Formatters.TryGetValue(o.GetType(), out formatter) == false)
            {
                formatter = DefaultFormatter;
            }

            var ret = formatter.Format(o);
            return ret;
        }

        /// <summary>
        /// Returns true if the system has a formatter registered for the given type
        /// </summary>
        /// <param name="t">the type to check</param>
        /// <returns>true if the system has a formatter registered for the given type, false otherwise</returns>
        public static bool HasFormatter(Type t)
        {
            return Formatters.ContainsKey(t);
        }

        /// <summary>
        /// Gets the registered formatter for the given type or null if there isn't one
        /// </summary>
        /// <param name="t">The formatter to get</param>
        /// <returns>the registered formatter for the given type or null if there isn't one</returns>
        public static IPipelineOutputFormatter GetFormatter(Type t)
        {
            return Formatters.ContainsKey(t) ? Formatters[t] : null;
        }

        /// <summary>
        /// Unregisters the formatter for the given type
        /// </summary>
        /// <param name="t">The type to unregister</param>
        public static void UnregisterFormatter(Type t)
        {
            if(Formatters.ContainsKey(t) == false)
            {
                throw new KeyNotFoundException("There is no formatter registered for type: "+t.FullName);
            }

            Formatters.Remove(t);
        }

        /// <summary>
        /// Registers a formatter for the given type
        /// </summary>
        /// <param name="t">The type to register for</param>
        /// <param name="formatter">The formatter implementation</param>
        /// <param name="allowOverride">If true, this method will allow overriding an existing formatter.  If false, this method throws an exception if a formatter is already registered for the given type.</param>
        public static void RegisterFormatter(Type t, IPipelineOutputFormatter formatter, bool allowOverride = false)
        {
            if(Formatters.ContainsKey(t) && allowOverride == false)
            {
                throw new InvalidArgDefinitionException("There is already a formatter registered for the type: "+t.FullName);
            }
            else if(Formatters.ContainsKey(t))
            {
                Formatters[t] = formatter;
            }
            else
            {
                Formatters.Add(t, formatter);
            }
        }
    }
}
