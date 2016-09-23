using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// Represents an XML attribute
    /// </summary>
    public class XmlAttribute : IXmlLineInfo
    {
        /// <summary>
        /// Gets the name of the attribute
        /// </summary>
        public string Name
        {
            get
            {
                return attribute.Name.LocalName;
            }
        }


        /// <summary>
        /// Gets the value of the attribute
        /// </summary>
        public string Value
        {
            get
            {
                return attribute.Value;
            }
        }

        private XAttribute attribute;

        /// <summary>
        /// gets the line number of the attribute from IXmlLineInfo.LineNumber
        /// </summary>
        public int LineNumber
        {
            get
            {
                return ((IXmlLineInfo)attribute).LineNumber;
            }
        }

        /// <summary>
        /// Gets the line position of the attribute from IXmlLineInfo.LinePosition
        /// </summary>
        public int LinePosition
        {
            get
            {
                return ((IXmlLineInfo)attribute).LinePosition;
            }
        }

        internal XmlAttribute(XAttribute attribute)
        {
            this.attribute = attribute;
        }

        /// <summary>
        /// Always returns true
        /// </summary>
        /// <returns>Always returns true</returns>
        public bool HasLineInfo()
        {
            return true;
        }
    }

    /// <summary>
    /// Represents an XML element
    /// </summary>
    public class XmlElement : IXmlLineInfo
    {
        private XElement node;

        /// <summary>
        /// Gets the name of the element
        /// </summary>
        public string Name
        {
            get
            {
                return node.Name.LocalName;
            }
        }

        /// <summary>
        /// gets the line number of the attribute from IXmlLineInfo.LineNumber
        /// </summary>
        public int LineNumber
        {
            get
            {
                return ((IXmlLineInfo)node).LineNumber;
            }
        }

        /// <summary>
        /// Gets the line position of the attribute from IXmlLineInfo.LinePosition
        /// </summary>
        public int LinePosition
        {
            get
            {
                return ((IXmlLineInfo)node).LinePosition;
            }
        }

        /// <summary>
        /// Always returns true
        /// </summary>
        /// <returns>Always returns true</returns>
        public bool HasLineInfo()
        {
            return true;
        }

        internal XmlElement(string xml)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(xml);
                writer.Flush();
                stream.Position = 0;
                XDocument xdoc = XDocument.Load(stream, LoadOptions.SetLineInfo);
                this.node = xdoc.Root;
            }
        }

        private XmlElement(XElement node)
        {
            this.node = node;
        }

        /// <summary>
        /// Gets the string value of the given attribute name or null if the attribute does not exist
        /// </summary>
        /// <param name="attributeName">the name of the attribute to inspect</param>
        /// <returns>the string value of the given attribute name or null if the attribute does not exist</returns>
        public string this[string attributeName]
        {
            get
            {
                return this.node.Attribute(XName.Get(attributeName, this.node.Name.NamespaceName))?.Value;
            }
        }

        /// <summary>
        /// Gets the child element at the given index
        /// </summary>
        /// <param name="index">the index</param>
        /// <returns>the child element at the given index</returns>
        public XmlElement this[int index]
        {
            get
            {
                return new XmlElement(this.node.Elements().ElementAt(index));
            }
        }
        
        /// <summary>
        /// Gets the child elements of this element
        /// </summary>
        public List<XmlElement> Elements
        {
            get
            {
                return this.node.Elements().Select(e => new XmlElement(e)).ToList();
            }
        }

        /// <summary>
        /// Gets the attributes that this element has
        /// </summary>
        public List<XmlAttribute> Attributes
        {
            get
            {
                return this.node.Attributes().Select(a => new XmlAttribute(a)).ToList();
            }
        }

        /// <summary>
        /// Gets the value of the given attribute name
        /// </summary>
        /// <typeparam name="T">The expected type of the attribute</typeparam>
        /// <param name="attributeName">the name of the attribute</param>
        /// <param name="parser">a parser to use, by default this method will try to find a static Parse method that accepts a single string argument and returns the proper type</param>
        /// <param name="defaultValue">optionally pass a default value to return if the attribute was not found</param>
        /// <returns>the value of the attribute parsed to the requested type, or a default value (null if no default provided)</returns>
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
