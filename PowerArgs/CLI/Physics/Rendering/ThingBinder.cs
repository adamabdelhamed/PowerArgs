using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs.Cli.Physics
{


    public class ThingBindingAttribute : Attribute
    {
        public Type ThingType { get; set; }
        public ThingBindingAttribute(Type t)
        {
            this.ThingType = t;
        }
    }

    public class ThingBinder
    {
        Dictionary<Type, Type> Bindings;

        public ThingBinder()
        {
            Bindings = LoadBindings();
        }

        public ThingRenderer Bind(Thing t)
        {
            Type binding;
            if (Bindings.TryGetValue(t.GetType(), out binding) == false)
            {
                binding = typeof(ThingRenderer);
            }

            ThingRenderer ret = Activator.CreateInstance(binding) as ThingRenderer;
            ret.Thing = t;
            return ret;
        }

        private Dictionary<Type, Type> LoadBindings()
        {
            Dictionary<Type, Type> ret = new Dictionary<Type, Type>();
            Assembly rendererAssembly = typeof(ThingRenderer).GetTypeInfo().Assembly;

            List<Type> rendererTypes = new List<Type>();

            foreach (Type t in from type in rendererAssembly.ExportedTypes where type.GetTypeInfo().IsSubclassOf(typeof(ThingRenderer)) select type)
            {
                if (t.GetTypeInfo().GetCustomAttributes(typeof(ThingBindingAttribute), true).Count() != 1) continue;
                rendererTypes.Add(t);
            }

            foreach (var thingAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in from type in thingAssembly.ExportedTypes where type.GetTypeInfo().IsSubclassOf(typeof(Thing)) select type)
                {
                    ret.Add(t, FindMatchingBinder(rendererTypes, t));
                }
            }

            return ret;
        }

        private Type FindMatchingBinder(List<Type> rendererTypes, Type thingType)
        {
            var match = (from renderer in rendererTypes where (renderer.GetTypeInfo().GetCustomAttributes(typeof(ThingBindingAttribute), true).First() as ThingBindingAttribute).ThingType == thingType select renderer).SingleOrDefault();
            if (match == null && thingType.GetTypeInfo().BaseType.GetTypeInfo().IsSubclassOf(typeof(Thing)))
            {
                return FindMatchingBinder(rendererTypes, thingType.GetTypeInfo().BaseType);
            }
            else if (match != null)
            {
                return match;
            }
            else
            {
                return typeof(ThingRenderer);
            }
        }
    }
}
