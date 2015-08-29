using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;

namespace PowerArgs
{
    /// <summary>
    /// A class that knows how to revive .NET objects from strings provided on the command line
    /// </summary>
    public static class ArgRevivers
    {
        private static Dictionary<Type, Func<string, string, object>> revivers;
        private static Dictionary<Type, Func<string, string, object>> Revivers
        {
            get
            {
                if (revivers == null)
                {
                    revivers = new Dictionary<Type, Func<string, string, object>>();
                    LoadDefaultRevivers(revivers);
                }
                return revivers;
            }
        }

        /// <summary>
        /// Returns true if the given type can be revived, false otherwise
        /// </summary>
        /// <param name="t">The type to test</param>
        /// <returns>true if the given type can be revived, false otherwise</returns>
        public static bool CanRevive(Type t)
        {
            if (Revivers.ContainsKey(t) ||
                t.IsEnum ||
                (t.GetInterfaces().Contains(typeof(IList)) && t.IsGenericType && CanRevive(t.GetGenericArguments()[0])) ||
                (t.IsArray && CanRevive(t.GetElementType())))
                return true;
            SearchAssemblyForRevivers(t.Assembly);

            if (System.ComponentModel.TypeDescriptor.GetConverter(t).CanConvertFrom(typeof(string))) return true;

            return Revivers.ContainsKey(t);
        }

        internal static object ReviveEnum(Type t, string value, bool ignoreCase)
        {
            if (value.Contains(","))
            {
                int ret = 0;
                var values = value.Split(',').Select(v => v.Trim());
                foreach (var enumValue in values)
                {
                    try
                    {
                        ret = ret | ParseEnumValue(t, enumValue, ignoreCase);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                        throw new ValidationArgException(enumValue + " is not a valid value for type " + t.Name + ", options are " + string.Join(", ", Enum.GetNames(t)));
                    }
                }

                return Enum.ToObject(t, ret);
            }
            else
            {
                try
                {
                    return Enum.ToObject(t, ParseEnumValue(t, value, ignoreCase));
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                    if (value == string.Empty) value = "<empty>";
                    throw new ValidationArgException(value + " is not a valid value for type " + t.Name + ", options are " + string.Join(", ", Enum.GetNames(t)));
                }
            }
        }

        private static int ParseEnumValue(Type t, string valueString, bool ignoreCase)
        {
            int rawInt;

            if (int.TryParse(valueString, out rawInt))
            {
                return (int)Enum.ToObject(t, rawInt);
            }

            object enumShortcutMatch;
            if (t.TryMatchEnumShortcut(valueString, ignoreCase, out enumShortcutMatch))
            {
                return (int)enumShortcutMatch;
            }

            return (int)Enum.Parse(t, valueString, ignoreCase);
        }

        /// <summary>
        /// Revives the given string into the desired .NET type
        /// </summary>
        /// <param name="t">The type to revive to</param>
        /// <param name="name">the name of the argument</param>
        /// <param name="value">The string value to revive</param>
        /// <returns>A revived object of the desired type</returns>
        public static object Revive(Type t, string name, string value)
        {
            if (t.IsArray == false && t.GetInterfaces().Contains(typeof(IList)))
            {
                var list = (IList)Activator.CreateInstance(t);
                // TODO - Maybe support custom delimiters via an attribute on the property
                // TODO - Maybe do a full parse of the value to check for quoted strings

                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    foreach (var element in value.Split(','))
                    {
                        list.Add(Revive(t.GetGenericArguments()[0], name + "_element", element));
                    }
                }
                return list;
            }
            else if (t.IsArray)
            {
                var elements = value.Split(',');

                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    Array array = Array.CreateInstance(t.GetElementType(), elements.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array.SetValue(Revive(t.GetElementType(), name + "[" + i + "]", elements[i]), i);
                    }
                    return array;
                }
                else
                {
                    return Array.CreateInstance(t.GetElementType(), 0);
                }
            }
            else if (Revivers.ContainsKey(t))
            {
                return Revivers[t].Invoke(name, value);
            }
            else if (System.ComponentModel.TypeDescriptor.GetConverter(t).CanConvertFrom(typeof(string)))
            {
                return System.ComponentModel.TypeDescriptor.GetConverter(t).ConvertFromString(value);
            }
            else
            {
                // Intentionally not an InvalidArgDefinitionException.  Other internal code should call 
                // CanRevive and this block should never be executed.
                throw new ArgumentException("Cannot revive type " + t.FullName + ". Callers should be calling CanRevive before calling Revive()");
            }
        }

        internal static void SearchAssemblyForRevivers(Assembly a)
        {
            foreach (var type in a.GetTypes())
            {
                var revivers = from m in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                               where m.HasAttr<ArgReviverAttribute>() &&
                                     m.GetParameters().Length == 2 &&
                                     m.GetParameters()[0].ParameterType == typeof(string) &&
                                     m.GetParameters()[1].ParameterType == typeof(string) &&
                                     m.ReturnType != typeof(void)
                               select m;

                foreach (var reviver in revivers)
                {
                    SetReviver(reviver);
                }
            }
        }

        private static void SetReviver(MethodInfo r)
        {
            SetReviver(r.ReturnType, (key, val) =>
            {
                return r.Invoke(null, new object[] { key, val });
            });
        }

        /// <summary>
        /// Sets the reviver function for a given type, overriding any existing reviver that may exist.
        /// </summary>
        /// <param name="t">The type of object the reviver function can revive</param>
        /// <param name="reviverFunc">the function that revives a command line string, converting it into a consumable object</param>
        public static void SetReviver(Type t, Func<string,string, object> reviverFunc)
        {
            if (ArgRevivers.Revivers.ContainsKey(t))
            {
                ArgRevivers.Revivers.Remove(t);
            }

            ArgRevivers.Revivers.Add(t, reviverFunc);
        }

        /// <summary>
        /// Sets the reviver function for a given type, overriding any existing reviver that may exist.
        /// </summary>
        /// <typeparam name="T">The type of object the reviver function can revive</typeparam>
        /// <param name="reviverFunc">the function that revives a command line string, converting it into a consumable object</param>
        public static void SetReviver<T>(Func<string, string, T> reviverFunc)
        {
            SetReviver(typeof(T), (k, v) => { return reviverFunc(k, v); });
        }

        internal static Func<string,string,object> GetReviver(Type t)
        {
            if (ArgRevivers.Revivers.ContainsKey(t))
            {
                return ArgRevivers.Revivers[t];
            }
            else
            {
                return null;
            }
        }

        private static void LoadDefaultRevivers(Dictionary<Type, Func<string, string, object>> revivers)
        {
            revivers.Add(typeof(bool), (prop, val) =>
            {
                return val != null && val.ToLower().ToString() != "false" && val != "0"; // null means the switch value was not specified.  If it was specified then it's automatically true
            });

            revivers.Add(typeof(Guid), (prop, val) =>
            {
                Guid ret;
                if (Guid.TryParse(val, out ret) == false) throw new FormatException("value must be a Guid: " + val);
                return ret;
            });

            revivers.Add(typeof(byte), (prop, val) =>
            {
                byte ret;
                if (byte.TryParse(val, out ret) == false) throw new FormatException("value must be a byte: " + val);
                return ret;
            });

            revivers.Add(typeof(int), (prop, val) =>
            {
                int ret;
                if (int.TryParse(val, out ret) == false) throw new FormatException("value must be an integer: " + val);
                return ret;
            });

            revivers.Add(typeof(long), (prop, val) =>
            {
                long ret;
                if (long.TryParse(val, out ret) == false) throw new FormatException("value must be an integer: " + val);
                return ret;
            });

            revivers.Add(typeof(double), (prop, val) =>
            {
                double ret;
                if (double.TryParse(val, out ret) == false) throw new FormatException("value must be a number: " + val);
                return ret;
            });

            revivers.Add(typeof(string), (prop, val) =>
            {
                return val;
            });

            revivers.Add(typeof(DateTime), (prop, val) =>
            {
                DateTime ret;
                if (DateTime.TryParse(val, out ret) == false) throw new FormatException("value must be a valid date time: " + val);
                return ret;
            });

            revivers.Add(typeof(SecureStringArgument), (prop, val) =>
            {
                if (val != null) throw new ArgException("The value for " + prop + " cannot be specified on the command line");
                return new SecureStringArgument(prop);
            });

            revivers.Add(typeof(Uri), (prop, val) =>
            {
                try
                {
                    return new Uri(val);
                }
                catch (UriFormatException)
                {
                    throw new UriFormatException("value must be a valid URI: " + val);
                }
            });

            revivers.Add(typeof(IPAddress), (prop, val) =>
            {
				System.Net.IPAddress ret;
				if (System.Net.IPAddress.TryParse(val, out ret) == false) throw new FormatException("value must be a valid IP address: " + val);
				return ret;
            });
        }
    }
}
