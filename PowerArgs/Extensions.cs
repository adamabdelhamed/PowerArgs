using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace PowerArgs
{
    public static class Extensions
    {

        public static bool HasAttr<T>(this MemberInfo info)
        {
            return info.GetCustomAttributes(typeof(T), true).Length > 0;
        }


        public static T Attr<T>(this MemberInfo info)
        {
            if (info.HasAttr<T>())
            {
                return (T)info.GetCustomAttributes(typeof(T), true)[0];
            }
            else
            {
                return default(T);
            }
        }


        public static List<T> Attrs<T>(this MemberInfo info)
        {
            if (info.HasAttr<T>())
            {
                return (from attr in info.GetCustomAttributes(typeof(T), true) select (T)attr).ToList();
            }
            else
            {
                return new List<T>();
            }
        }
    }
}
