using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace PowerArgs.Cli
{
    public class XmlAttribute
    {
        public string Name { get; private set; }
        public string Value { get; private set; }

        public XmlAttribute(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }
    }

    public class XmlElement
    {
        private XElement node;

        public string Name
        {
            get
            {
                return node.Name.LocalName;
            }
        }

        public XmlElement(string xml)
        {
            this.node = XElement.Parse(xml);
        }

        private XmlElement(XElement node)
        {
            this.node = node;
        }

        public string this[string attributeName]
        {
            get
            {
                return this.node.Attribute(XName.Get(attributeName, this.node.Name.NamespaceName))?.Value;
            }
        }

        public XmlElement this[int index]
        {
            get
            {
                return new XmlElement(this.node.Elements().ElementAt(index));
            }
        }

        public List<XmlElement> Elements
        {
            get
            {
                return this.node.Elements().Select(e => new XmlElement(e)).ToList();
            }
        }

        public List<XmlAttribute> Attributes
        {
            get
            {
                return this.node.Attributes().Select(a => new XmlAttribute(a.Name.LocalName, a.Value)).ToList();
            }
        }


        public T? Attribute<T>(string attributeName, Func<string, T> parser = null, T? defaultValue = null) where T : struct
        {
            parser = parser ?? new Func<string, T>((str) =>
            {
                var parseMethod = typeof(T).GetMethod("Parse", new Type[] { typeof(string) });
                var parsed = (T)parseMethod.Invoke(null, new object[] { str });
                return parsed;
            });

            var raw = this[attributeName];
            if (raw == null)
            {
                return defaultValue;
            }
            var ret = parser(raw);

            return ret;
        }
    }
}
