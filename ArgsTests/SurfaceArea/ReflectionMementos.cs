using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Json;

namespace ArgsTests.SurfaceArea
{
    public static class Poco
    {
        public static void CopyInto(this object from, object to)
        {
            if (from == null || to == null) return;

            foreach (PropertyInfo fromProp in from.GetType().GetProperties())
            {
                PropertyInfo toProp = to.GetType().GetProperty(fromProp.Name);
                if (toProp == null || toProp.PropertyType != fromProp.PropertyType) continue;
                if (fromProp.CanRead == false || toProp.CanWrite == false) continue;

                var val = fromProp.GetValue(from, null);
                toProp.SetValue(to, val, null);
            }
        }

        public static List<string> PocoDiff(this object a, object b)
        {
            if (a == null ^ b == null)
            {
                return new List<string>();
            }
            else if (a == null)
            {
                return new List<string>();
            }

            return a.PocoDiffInternal(b, a.GetType().Name);
            
        }

        private static List<string> PocoDiffInternal(this object a, object b, string context = "")
        {
            if (a == null ^ b == null)
            {
                return new List<string>();
            }
            else if (a == null)
            {
                return new List<string>();
            }

            if (a is string ||
               a is char ||
               a is long ||
               a is short ||
               a is int ||
               a is byte ||
               a is double ||
               a is float ||
               a is bool ||
               a is Guid ||
               a is DateTime ||
               a is TimeSpan ||
               a.GetType().IsEnum)
            {
                if (a.Equals(b))
                {
                    return new List<string>();
                }
                else
                {
                    return new List<string>() { context + " - " + string.Format("A = '{0}', B = '{1}'", a, b) };
                }
            }

            List<string> diffs = new List<string>();

            var aType = a.GetType();
            foreach (PropertyInfo aProp in aType.GetProperties())
            {
                var newContext = context + "." + aProp.Name;
                PropertyInfo bProp = b.GetType().GetProperty(aProp.Name);
                if (bProp == null || bProp.PropertyType != aProp.PropertyType) continue;
                if (aProp.CanRead == false || bProp.CanRead == false) continue;

                var aVal = aProp.GetValue(a, null);
                var bVal = bProp.GetValue(b, null);

                if (aVal == null ^ bVal == null)
                {
                    diffs.Add(newContext + " - " + string.Format("A = '{0}', B = '{1}'", aVal, bVal));
                    continue;
                }
                if (aVal == null) continue;

                if (aProp.PropertyType.GetInterfaces().Contains(typeof(IList)))
                {
                    IList aList = aVal as IList;
                    IList bList = bVal as IList;

                    if (aList.Count != bList.Count)
                    {
                        diffs.Add(newContext + " - " + string.Format("Objects have different counts ({0}, {1})", aList.Count, bList.Count));
                        continue;
                    }

                    for (int i = 0; i < aList.Count; i++)
                    {
                        diffs.AddRange(aList[i].PocoDiffInternal(bList[i], newContext+"["+i+"]"));
                    }
                }
                else
                {
                    diffs.AddRange(aVal.PocoDiffInternal(bVal, newContext));
                }
            }

            return diffs;
        }
    }

    public class AssemblyDef
    {
        public string FullName { get; set; }
        public List<TypeDef> Types { get; set; }

        public AssemblyDef() { }

        public AssemblyDef(Assembly a)
        {
            a.CopyInto(this);
            Types = (from t in a.GetTypes() where t.IsPublic select new TypeDef(t)).ToList();
        }
    }

    public class TypeDef
    {
        public string AssemblyQualifiedName { get; set; }
        public string FullName { get; set; }
        public string Name { get; set; }
        public bool IsVisible { get; set; }
        public bool IsEnum { get; set; }
        public bool IsValueType { get; set; }
        public bool IsGenericType { get; set; }

        public List<string> GenericArgumentAssemblyQualifiedNames { get; set; }
        public List<string> CustomAttributeAssemblyQualifiedNames { get; set; }
        public List<PropertyDef> Properties { get; set; }
        public List<MethodDef> Methods { get; set; }
        public List<FieldDef> Fields { get; set; }
        public List<string> EnumNames { get; set; }
        public List<int> EnumValues { get; set; }

        // TODO - Events

        public TypeDef() { }

        public TypeDef(Type t)
        {
            t.CopyInto(this);
            GenericArgumentAssemblyQualifiedNames = (from arg in t.GetGenericArguments() select arg.AssemblyQualifiedName).ToList();
            CustomAttributeAssemblyQualifiedNames = (from attr in t.GetCustomAttributes(true) select attr.GetType().AssemblyQualifiedName).ToList();

            if (t.IsEnum)
            {
                EnumValues = new List<int>();
                EnumNames = new List<string>();
                foreach (var name in Enum.GetNames(t))
                {
                    EnumNames.Add(name);
                    EnumValues.Add((int)Enum.Parse(t, name));
                }
                return;
            }

            Methods = (from m in t.GetMethods() select new MethodDef(m)).ToList();

            Fields = new List<FieldDef>();
            Fields.AddRange(from f in t.GetFields(BindingFlags.Public | BindingFlags.Static) select new FieldDef(f));
            Fields.AddRange(from f in t.GetFields(BindingFlags.Public | BindingFlags.Instance) select new FieldDef(f));

            Properties = new List<PropertyDef>();
            Properties.AddRange(from p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance) select new PropertyDef(p, BindingFlags.Public | BindingFlags.Instance));
            Properties.AddRange(from p in t.GetProperties(BindingFlags.Public | BindingFlags.Static) select new PropertyDef(p, BindingFlags.Public | BindingFlags.Static));
        }
    }

    public class PropertyDef
    {
        public string Name { get; set; }
        
        public string PropertyTypeAssemblyQualifiedName { get; set; }
        public string DeclaringTypeTypeAssemblyQualifiedName { get; set; }
        public string ReflectedTypeTypeAssemblyQualifiedName { get; set; }
        public BindingFlags BindingFlags { get; set; }

        public MethodDef GetMethod { get; set; }
        public MethodDef SetMethod { get; set; }

        public PropertyDef() { }

        public PropertyDef(PropertyInfo p, BindingFlags flags)
        {
            p.CopyInto(this);

            BindingFlags = flags;
            PropertyTypeAssemblyQualifiedName = p.PropertyType.AssemblyQualifiedName;
            DeclaringTypeTypeAssemblyQualifiedName = p.DeclaringType.AssemblyQualifiedName;
            ReflectedTypeTypeAssemblyQualifiedName = p.ReflectedType.AssemblyQualifiedName;
            GetMethod = p.GetGetMethod() == null ? null : new MethodDef(p.GetGetMethod());
            SetMethod = p.GetSetMethod() == null ? null :  new MethodDef(p.GetSetMethod());
        }
    }

    public class MethodDef
    {
        public string Name { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public bool IsVirtual { get; set; }

        public string DeclaringTypeTypeAssemblyQualifiedName { get; set; }
        public string ReflectedTypeTypeAssemblyQualifiedName { get; set; }
        public List<string> ParameterTypes { get; set; }
        public List<string> GenericArgumentAssemblyQualifiedNames { get; set; }
        public string ReturnTypeAssemblyQualifiedName { get; set; }

        public MethodDef() { }

        public MethodDef(MethodInfo m)
        {
            m.CopyInto(this);

            DeclaringTypeTypeAssemblyQualifiedName = m.DeclaringType.AssemblyQualifiedName;
            ReflectedTypeTypeAssemblyQualifiedName = m.ReflectedType.AssemblyQualifiedName;
            ReturnTypeAssemblyQualifiedName = m.ReturnType.AssemblyQualifiedName;
            ParameterTypes = (from p in m.GetParameters() select p.ParameterType.AssemblyQualifiedName).ToList();
            GenericArgumentAssemblyQualifiedNames = (from arg in m.GetGenericArguments() select arg.AssemblyQualifiedName).ToList();
        }
    }

    public class FieldDef
    {
        public string Name { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }

        public string FieldTypeAssemblyQualifiedName { get; set; }
        public string DeclaringTypeTypeAssemblyQualifiedName { get; set; }
        public string ReflectedTypeTypeAssemblyQualifiedName { get; set; }

        public FieldDef() { }

        public FieldDef(FieldInfo f)
        {
            f.CopyInto(this);

            FieldTypeAssemblyQualifiedName = f.FieldType.AssemblyQualifiedName;
            DeclaringTypeTypeAssemblyQualifiedName = f.DeclaringType.AssemblyQualifiedName;
            ReflectedTypeTypeAssemblyQualifiedName = f.ReflectedType.AssemblyQualifiedName;
        }
    }
}
