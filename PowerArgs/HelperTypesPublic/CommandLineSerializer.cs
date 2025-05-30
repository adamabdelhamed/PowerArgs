using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace PowerArgs
{
    /// <summary>
    /// An attribute that can be used to override how an argument is serialized
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class ArgSerializer : Attribute, ICommandLineArgumentMetadata
    {
        /// <summary>
        /// the default serialization method. When overriden the derived class controls the conversion.
        /// </summary>
        /// <param name="value">the object to serialize</param>
        /// <returns>the serialized object</returns>
        public virtual string Serialize(object value)
        {
            if (value is ConsoleString)
            {
                return (value as ConsoleString).Serialize();
            }

            var ret = "" + value;

            if (ret == "∞")
            {
                ret = "PositiveInfinity";
            }

            return ret;
        }
    }

    /// <summary>
    /// An attribute that tells the CommandLineSerializer to not serialize the target argument if it matches the specified value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class ArgIgnoreSerializeAttribute : Attribute, ICommandLineArgumentMetadata
    {
        /// <summary>
        /// The value to not serialize
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">the value to never serialize</param>
        public ArgIgnoreSerializeAttribute(object value) => this.Value = value;
    }

    /// <summary>
    /// A tool that lets you convert an object into a serialized command string that can be fed back
    /// into the Args parser.
    /// </summary>
    public static class CommandLineSerializer
    {
        /// <summary>
        /// Serializes the given object to a string given a set of argument definitions
        /// </summary>
        /// <param name="o">the object to serialize</param>
        /// <param name="arguments">the arguments to consult for metadata about the serialization process.</param>
        /// <returns></returns>
        public static string Serialize(object o, List<CommandLineArgument> arguments)
        {
            var ret = "";
            var validProps = new Dictionary<string, CommandLineArgument>(StringComparer.OrdinalIgnoreCase);
            foreach (var arg in arguments)
            {
                var source = arg.Source as PropertyInfo;
                if (source == null)
                {
                    throw new NotSupportedException("Argument source must be properties");
                }
                else if (source.Name.Equals(arg.DefaultAlias, StringComparison.OrdinalIgnoreCase) == false)
                {
                    throw new NotSupportedException("Default alias must be property name");
                }

                validProps.Add(arg.DefaultAlias, arg);
            }

            var defaultSerializer = new ArgSerializer();
            foreach (var property in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.HasAttr<ArgIgnoreAttribute>() == false && p.GetGetMethod() != null && p.GetSetMethod() != null))
            {
                var val = property.GetValue(o);
                if (val == null) continue;

                if (validProps.TryGetValue(property.Name, out CommandLineArgument arg))
                {
                    var serializer = arg.Metadata.TryGetMeta(out ArgSerializer customSerializer) ? customSerializer : defaultSerializer;
                    if (val is IList || property.PropertyType.IsArray)
                    {
                        var list = $"-{arg.DefaultAlias} \"";
                        foreach (var item in (IEnumerable)val)
                        {
                            var serializedValue = serializer.Serialize(item);
                            if (serializedValue.Contains(","))
                            {
                                throw new NotSupportedException("Serialized list items can't have commas");
                            }
                            serializedValue = serializedValue.Replace("\"", "\\\"");
                            list += serializedValue + ",";
                        }

                        if (list.EndsWith(","))
                        {
                            list = list.Substring(0, list.Length - 1);
                        }
                        list += "\" ";

                        ret += list;
                    }
                    else
                    {
                        if (property.PropertyType == typeof(bool) && (bool)val == false)
                        {
                            continue;
                        }

                        var serializedValue = serializer.Serialize(val);

                        if (arg.Metadata.TryGetMeta<ArgDefaultValueAttribute, ICommandLineArgumentMetadata>(out ArgDefaultValueAttribute defaultValAttr))
                        {
                            if (serializer.Serialize(defaultValAttr.Value).Equals(serializedValue, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        var ignore = false;


                        var ignoreAttrs = new List<ArgIgnoreSerializeAttribute>();
                        for (int i = 0; i < arg.Metadata.Count; i++)
                        {
                            if (arg.Metadata[i] is ArgIgnoreSerializeAttribute)
                            {
                                ignoreAttrs.Add(arg.Metadata[i] as ArgIgnoreSerializeAttribute);
                            }
                        }


                        foreach (var meta in ignoreAttrs)
                        {
                            if (serializer.Serialize(meta.Value).Equals(serializedValue, StringComparison.OrdinalIgnoreCase))
                            {
                                ignore = true;
                                break;
                            }
                        }

                        if (ignore) continue;

                        serializedValue = serializedValue.Replace("\"", "\\\"");
                        if (serializedValue.Where(c => char.IsWhiteSpace(c)).Any())
                        {
                            serializedValue = "\"" + serializedValue + "\"";
                        }

                        if (property.PropertyType == typeof(bool))
                        {
                            ret += $"-{arg.DefaultAlias} ";
                        }
                        else
                        {
                            ret += $"-{arg.DefaultAlias} {serializedValue} ";
                        }
                    }
                }
            }
            ret = ret.Trim();
            return ret;
        }
    }
}
