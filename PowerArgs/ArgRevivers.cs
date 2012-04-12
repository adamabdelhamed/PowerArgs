using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace PowerArgs
{
    public static class ArgRevivers
    {
        static Dictionary<Type, Func<string, string, object>> revivers;
        internal static Dictionary<Type, Func<string, string, object>> Revivers
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
                    var r = reviver;
                    if (ArgRevivers.Revivers.ContainsKey(r.ReturnType) == false)
                    {
                        ArgRevivers.Revivers.Add(r.ReturnType, (key, val) =>
                        {
                            return r.Invoke(null, new object[] { key, val });
                        });
                    }
                }
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
                if (DateTime.TryParse(val, out ret) == false) throw new ArgumentException("value must be a valid date time: " + val);
                return ret;
            });
        }
    }
}
