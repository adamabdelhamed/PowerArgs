using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PowerArgs.Games
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IMultiPlayerEventIgnore : Attribute { }
    public abstract class MultiPlayerMessage
    {
        private static Type[] SupportedDataTypes = new Type[] 
        {
            typeof(string),
            typeof(int),
            typeof(float),
            typeof(bool)
        };

        public string Sender { get; set; }
        public string Recipient { get; set; }
        public string RequestId { get; set; }
        private string Enc(string val) => val == null ? "null" : val.Replace("/", "-");

        public string Serialize()
        {
            var messageContents = Base64Encode(GetType().Name);
            messageContents += "\n" + Base64Encode(nameof(Sender)) + ":" + Base64Encode(Sender);
            messageContents += "\n" + Base64Encode(nameof(Recipient)) + ":" + Base64Encode(Recipient);

            if(RequestId != null)
            {
                messageContents += "\n" + Base64Encode(nameof(RequestId)) + ":" + Base64Encode(RequestId);
            }

            foreach (var property in GetType().GetProperties().Where(p => p.HasAttr<IMultiPlayerEventIgnore>() == false))
            {
                AssertSupported(property.PropertyType);
                var val = property.GetValue(this);
                string stringVal;
                if(property.PropertyType == typeof(string))
                {
                    stringVal = (string)val;
                }
                else  
                {
                    stringVal = val.ToString();
                }

                messageContents += "\n" + Base64Encode(property.Name) + ":" + Base64Encode(stringVal);
            }
            return messageContents;
        }

        public static MultiPlayerMessage Deserialize(string message)
        {
            var lines = message.Split('\n');
            var type = Base64Decode(lines[0]);
            var ret = ObjectFactory.CreateInstance<MultiPlayerMessage>(type);

            for (var i = 1; i < lines.Length; i++)
            {
                var split = lines[i].Split(':');
                var key = Base64Decode(split[0]);
                var value = Base64Decode(split[1]);

                var prop = ret.GetType().GetProperty(key);

                if(prop.PropertyType == typeof(string))
                {
                    prop.SetValue(ret, value);
                }
                else
                {
                    var parseMethod = prop.PropertyType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                        .Where(p => p.Name == nameof(int.Parse) && p.GetParameters().Length == 1 && p.GetParameters()[0].ParameterType == typeof(string)).Single();
                    prop.SetValue(ret, parseMethod.Invoke(null, new object[] { value }));
                }
            }

            return ret;
        }

        private void AssertSupported(Type t)
        {
            if (SupportedDataTypes.Contains(t) == false)
            {
                throw new NotSupportedException($"Type '{t.FullName}' not supported");
            }
        }

        private static string Base64Encode(string plainText)
        {
            if (plainText == null) return "$null";
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static string Base64Decode(string base64EncodedData)
        {
            if (base64EncodedData == "$null")
            {
                return null;
            }
            else
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
        }
    }
}
