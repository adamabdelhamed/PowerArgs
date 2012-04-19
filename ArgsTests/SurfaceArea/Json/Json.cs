using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.IO;

namespace JSON
{
    public class Reviver
    {
        public Func<Type, string, string, object> Revive { get; set; }
    }

    public class Json
    {
        private static int MaxDepth = 20;
        private static string keyChars = ",:[]{}";

        private static List<Reviver> builtInRevivers = LoadDefaultRevivers();

        private bool isOnANewLine = true;
        private bool useQuotesAroundAllItems;
        private bool format = true;
        private string defaultIndent;
        private string newLine;
        private Stack<Type> reviveTypeStack;
        private Stack<string> revivePropertyStack;
        private List<Reviver> revivers = new List<Reviver>();
        private Json() { }

        public static string Stringify(object o)
        {
            return Stringify(o, true, true, true);
        }

        public static string Stringify(object o, bool convertEnumerablesToArrays, bool useQuotesAroundAllItems, bool format)
        {
            Json worker = new Json();
            worker.defaultIndent = format ? "    " : "";
            worker.newLine = format ? "\n" : "";
            worker.useQuotesAroundAllItems = useQuotesAroundAllItems;
            return worker.StringifyObject(o, "", convertEnumerablesToArrays, 0).Replace("\n\n", "\n").Trim();
        }

        public static dynamic Parse(string text, List<Reviver> revivers = null)
        {
            Json worker = new Json();
            return worker.ParseInternal(text, revivers);
        }

        public static T Parse<T>(string text, List<Reviver> revivers = null)
        {
            return (T)Parse(typeof(T), text, revivers);
        }

        public static object Parse(Type t, string text, List<Reviver> revivers = null)
        {
            var ret = Activator.CreateInstance(t);
            Parse(ref ret, text, revivers);
            return ret;
        }

        public static void Parse(ref object ret, string text, List<Reviver> revivers = null)
        {
            Json worker = new Json();
            worker.reviveTypeStack = new Stack<Type>();
            worker.revivePropertyStack = new Stack<string>();
            worker.reviveTypeStack.Push(ret.GetType());

            var parsed = worker.ParseInternal(text, revivers);
            if (parsed is JSONObject)
            {
                Type listType = null;
                if (ret is IList && ret.GetType().GetGenericArguments().Length == 1)
                {
                    listType = ret.GetType().GetGenericArguments()[0];
                }
                ProjectObjectTo(parsed, ref ret, listType);
            }
            else
            {
                ret = parsed;
            }
        }



        // Stringify Private Methods

        private string StringifyObject(object o, string indent, bool convertEnumerablesToArrays, int depth)
        {
            if (depth >= MaxDepth || o == null) return useQuotesAroundAllItems ? UnescapeLiteral("null") : "null";

            if (o is byte ||
                o is sbyte ||
                o is short ||
                o is ushort ||
                o is int ||
                o is long ||
                o is ulong ||
                o is float ||
                o is double ||
                o is bool ||
                o is float ||
                o is char ||
                o is string ||
                o is DateTime ||
                o is Guid ||
                (o is JSONObject && ((JSONObject)o).Type == JSONObject.JSONType.Simple))
            {
                string conditionalIndent = isOnANewLine ? indent : "";
                isOnANewLine = false;

                bool useQuotes = useQuotesAroundAllItems ||
                                  o is char ||
                                  o is string ||
                                  o is DateTime ||
                                  o is Guid ||
                                 (o is JSONObject && ((JSONObject)o).Value is char) ||
                                 (o is JSONObject && ((JSONObject)o).Value is string) ||
                                 (o is JSONObject && ((JSONObject)o).Value is DateTime) ||
                                 (o is JSONObject && ((JSONObject)o).Value is Guid);

                if (o is DateTime || (o is JSONObject && ((JSONObject)o).Value is DateTime))
                {
                    DateTime toStringify = o is JSONObject ? (DateTime)((JSONObject)o).Value :  ((DateTime)o).ToUniversalTime();
                    TimeSpan diff = toStringify - new DateTime(1970, 1, 1, 0,0,0,DateTimeKind.Utc);
                    long ms = (long) diff.TotalMilliseconds;
                    o = "/Date(" + ms + ")/";
                }

                return conditionalIndent + UnescapeLiteral(o, useQuotes);
            }

            if (convertEnumerablesToArrays && o is IEnumerable && o.GetType().IsArray == false && o is JSONObject == false)
            {
                var expanded = new List<Object>();
                foreach (var child in (IEnumerable)o) expanded.Add(child);
                o = expanded.ToArray();
            }

            if (o.GetType().IsArray || (o is JSONObject && ((JSONObject)o).Type == JSONObject.JSONType.List))
            {
                return StringifyArray(o, indent, convertEnumerablesToArrays, depth);
            }
            else
            {
                return StringifyObjectProperties(o, indent, convertEnumerablesToArrays, depth);
            }
        }

        private string StringifyArray(object array, string indent, bool convertEnumerablesToArrays, int depth)
        {
            string ret = newLine + indent + "[" + newLine;
            isOnANewLine = true;

            PropertyInfo target = array.GetType().GetProperty("Length") ?? array.GetType().GetProperty("Count");
            int len = (int)target.GetValue(array, null);

            int i = 0;
            foreach (var child in array as IEnumerable)
            {
                if (i < len - 1)
                {
                    ret += StringifyObject(child, indent + "    ", convertEnumerablesToArrays, depth + 1) + "," + newLine;
                    isOnANewLine = true;
                }
                else
                {
                    ret += StringifyObject(child, indent + "    ", convertEnumerablesToArrays, depth + 1) + newLine;
                    isOnANewLine = true;
                }
                i++;
            }
            ret += indent + "]";
            return ret;
        }

        private string StringifyObjectProperties(object obj, string indent, bool convertEnumerablesToArrays, int depth)
        {
            if (obj.GetType().GetProperties().Length == 0)
            {
                isOnANewLine = false;
                return indent + "\"" + obj.ToString() + "\"";
            }

            string ret = newLine + indent + "{" + newLine;
            isOnANewLine = true;
            int i = 0;
            if (obj is JSONObject)
            {
                JSONObject dy = obj as JSONObject;
                foreach (var prop in dy)
                {
                    object val = dy[prop];
                    if (i++ < dy.Count - 1)
                    {
                        isOnANewLine = false;
                        ret += indent + defaultIndent + UnescapeLiteral(prop, useQuotesAroundAllItems) + ":" + StringifyObject(dy[prop], indent + defaultIndent, convertEnumerablesToArrays, depth + 1) + "," + newLine;
                        isOnANewLine = true;
                    }
                    else
                    {
                        isOnANewLine = false;
                        ret += indent + defaultIndent + UnescapeLiteral(prop, useQuotesAroundAllItems) + ":" + StringifyObject(dy[prop], indent + defaultIndent, convertEnumerablesToArrays, depth + 1) + newLine;
                        isOnANewLine = true;
                    }
                }
            }
            else
            {
                foreach (PropertyInfo prop in obj.GetType().GetProperties())
                {
                    var val = prop.GetValue(obj, null);
                    if (i++ < obj.GetType().GetProperties().Length - 1)
                    {
                        isOnANewLine = false;
                        ret += indent + defaultIndent + UnescapeLiteral(prop.Name, useQuotesAroundAllItems) + ":" + StringifyObject(val, indent + defaultIndent, convertEnumerablesToArrays, depth + 1) + "," + newLine;
                        isOnANewLine = true;
                    }
                    else
                    {
                        isOnANewLine = false;
                        ret += indent + defaultIndent + UnescapeLiteral(prop.Name, useQuotesAroundAllItems) + ":" + StringifyObject(val, indent + defaultIndent, convertEnumerablesToArrays, depth + 1) + newLine;
                        isOnANewLine = true;
                    }
                }
            }
            ret += indent + "}";
            return ret;
        }

        // Parser Private Methods

        private dynamic ParseInternal(string text, List<Reviver> revivers = null)
        {
            if (revivers != null)
            {
                this.revivers.AddRange(revivers);
            }

            this.revivers.AddRange(builtInRevivers);

            if (string.IsNullOrWhiteSpace(text)) return new JSONObject(JSONObject.JSONType.Simple) { Value = "" };
            var tokens = Tokenize(text);
            var ret = ParseJSON(tokens);
            if (ret.Type == JSONObject.JSONType.Simple)
            {
                return ret.Value;
            }
            else
            {
                return ret;
            }
        }

        private JSONObject ParseJSON(Queue<string> tokens)
        {
            string token = tokens.Dequeue();
            if (token == "[") return ParseArray(tokens);
            else if (token == "{") return ParseObject(tokens);
            else return ParseLiteral(token);
        }

        private JSONObject ParseArray(Queue<string> tokens)
        {
            if (CurrentType != null)
            {
                revivePropertyStack.Push(null);
                reviveTypeStack.Push(CurrentType.GetGenericArguments()[0]);
            }

            JSONObject ret = new JSONObject(JSONObject.JSONType.List);
            while (true)
            {
                string token = tokens.Dequeue();
                if (token == "[") ret.Elements.Add(ParseArray(tokens));
                else if (token == "{") ret.Elements.Add(ParseObject(tokens));
                else if (token == "]") break;
                else ret.Elements.Add(ParseLiteral(token));

                if ((token = tokens.Dequeue()) == "]") break;
                if (token != ",") throw new Exception("expected ',' got " + token);
            }

            if (CurrentType != null)
            {
                revivePropertyStack.Pop();
                reviveTypeStack.Pop();
            }

            return ret;
        }

        private JSONObject ParseObject(Queue<string> tokens)
        {
            JSONObject ret = new JSONObject(JSONObject.JSONType.Object);
            while (true)
            {
                string propName = ParseIdentifier(tokens.Dequeue());
                PushProperty(propName);
                if (tokens.Dequeue() != ":") throw new Exception("Expected :");
                ret[propName] = ParseJSON(tokens);
                PopProperty();
                var next = tokens.Dequeue();
                if (next == "}") break;
                else if (next != ",") throw new Exception("expected , got " + next);
            }
            return ret;
        }

        private JSONObject ParseLiteral(string token)
        {
            if (token.StartsWith("\"") && token.EndsWith("\"")) token = token.Substring(1, token.Length - 2);

            JSONObject ret = new JSONObject(JSONObject.JSONType.Simple);
            ret.Value = token;
            if (token == "null") ret.Value = null;

            foreach (Reviver reviver in revivers)
            {
                var obj = reviver.Revive(CurrentType, CurrentProperty, token);
                if (obj != null)
                {
                    ret.Value = obj;
                    break;
                }
            }

            return ret;
        }

        private string ParseIdentifier(string token)
        {
            if (token.StartsWith("\"") && token.EndsWith("\"")) token = token.Substring(1, token.Length - 2);

            foreach (char c in token)
            {
                if (char.IsLetterOrDigit(c) == false)
                {
                    throw new Exception("Identifiers must be alphanumeric");
                }
            }

            return token;
        }

        private Type CurrentType
        {
            get
            {
                return reviveTypeStack != null && reviveTypeStack.Count > 0 ? reviveTypeStack.Peek() : null;
            }
        }

        private string CurrentProperty
        {
            get
            {
                return revivePropertyStack != null && revivePropertyStack.Count > 0 ? revivePropertyStack.Peek() : null;
            }
        }

        private void PushProperty(string prop)
        {
            if (revivePropertyStack != null)
            {
                PushType(CurrentType.GetProperty(prop).PropertyType);
                revivePropertyStack.Push(prop);
            }
        }

        private void PushType(Type t)
        {
            if (reviveTypeStack != null)
            {
                reviveTypeStack.Push(t);
            }
        }

        private string PopProperty()
        {
            if (revivePropertyStack != null)
            {
                PopType();
                return revivePropertyStack.Pop();
            }
            else
            {
                return null;
            }
        }

        private Type PopType()
        {
            if (reviveTypeStack != null)
            {
                return reviveTypeStack.Pop();
            }
            else
            {
                return null;
            }
        }

        private static void ProjectObjectTo(JSONObject parsed, ref object ret, Type collectionType = null)
        {
            if (collectionType != null && parsed.Type == JSONObject.JSONType.List)
            {
                foreach (var obj in parsed)
                {
                    object toProject;
                    if (obj is JSONObject)
                    {
                        toProject = Activator.CreateInstance(collectionType);
                        ProjectObjectTo((JSONObject)obj, ref toProject, toProject is IList ? toProject.GetType() : null);
                    }
                    else
                    {
                        toProject = obj;
                    }
                    (ret as IList).Add(toProject);
                }
            }
            else if (parsed.Type == JSONObject.JSONType.Simple)
            {
                if (ret.GetType() == typeof(double) && parsed.Value is int)
                {
                    ret = ((int)parsed.Value) + 0.0;
                }
                else
                {
                    ret = parsed.Value;
                }
            }
            else
            {
                foreach (PropertyInfo prop in ret.GetType().GetProperties())
                {
                    if (parsed.Properties.ContainsKey(prop.Name))
                    {
                        var val = parsed[prop.Name];
                        if (val.Type == JSONObject.JSONType.Simple)
                        {
                            if (prop.PropertyType.IsEnum)
                            {
                                prop.SetValue(ret, Enum.Parse(prop.PropertyType, val.Value), null);
                            }
                            else
                            {
                                prop.SetValue(ret, val.Value, null);
                            }
                        }
                        else if (val.Type == JSONObject.JSONType.Object)
                        {
                            var toProjectNext = Activator.CreateInstance(prop.PropertyType);
                            ProjectObjectTo(val, ref toProjectNext);
                            prop.SetValue(ret, toProjectNext, null);
                        }
                        else if (val.Type == JSONObject.JSONType.List)
                        {
                            var toProjectNext = Activator.CreateInstance(prop.PropertyType);
                            ProjectObjectTo(val, ref toProjectNext, prop.PropertyType.GetGenericArguments()[0]);
                            prop.SetValue(ret, toProjectNext, null);
                        }
                    }
                }
            }
        }

        // Tokenizer Private Methods

        private Queue<string> Tokenize(string s)
        {
            Queue<string> ret = new Queue<string>();

            int i = 0;
            while (true)
            {
                if (i == s.Length) break;
                char c = s[i++];
                if (char.IsWhiteSpace(c)) continue;

                if (keyChars.Contains(c)) ret.Enqueue(c + "");
                else if (c == '\"') ret.Enqueue(GetQuotedToken(s, ref i));
                else
                {
                    i--;
                    ret.Enqueue(GetIdentifierOrLiteralToken(s, ref i));
                }
            }
            return ret;
        }

        private string GetQuotedToken(string text, ref int index)
        {
            string ret = "\"";
            char c = text[index++];
            char last = (char)0;
            while (c != '"' || last == '\\')
            {
                if (c == '\\' && last != '\\')  // Detect a new escape sequence
                {
                    last = '\\';
                }
                else if (last == '\\')          // Perform the escape
                {
                    last = (char)0;
                    ret += EscapeChar(c);
                }
                else                            // A regular character
                {
                    ret += c;
                    last = c;
                }
                c = text[index++];
            }
            ret += "\"";
            return ret;
        }

        private string GetIdentifierOrLiteralToken(string text, ref int index)
        {
            string ret = "";
            char c = text[index++];
            while (char.IsWhiteSpace(c) == false && keyChars.Contains(c) == false)
            {
                ret += c;
                if (index == text.Length) return ret;
                c = text[index++];
            }
            index--;
            return ret;
        }

        private string EscapeChar(char c)
        {
            if (c == 'n') return "\n";
            else if (c == 'r') return "\r";
            else if (c == 't') return "\t";
            else if (c == '\\') return "\\";
            else if (c == '"') return "\"";
            else return c + "";
        }

        private string UnescapeChar(char c)
        {
            if (c == '\n') return "\\n";
            else if (c == '\r') return "\\r";
            else if (c == '\t') return "\\t";
            else if (c == '\\') return "\\\\";
            else if (c == '"') return "\\\"";
            else return c + "";
        }

        private string UnescapeLiteral(object text, bool encloseInQuotes = true)
        {
            string val = text.ToString();


            if (encloseInQuotes)
            {
                string ret = "";
                foreach (char c in val)
                {
                    ret += UnescapeChar(c);
                }

                ret = "\"" + ret + "\"";
                return ret;
            }
            else
            {
                return text.ToString();
            }
        }

        private static List<Reviver> LoadDefaultRevivers()
        {
            List<Reviver> ret = new List<Reviver>();

            ret.Add(new Reviver()
            {
                Revive = (type, key, value) =>
                {
                    int intVal;
                    if (int.TryParse(value, out intVal)) return intVal;
                    else return null;
                }
            });

            ret.Add(new Reviver()
            {
                Revive = (type, key, value) =>
                {
                    double doubleVal;
                    if (double.TryParse(value, out doubleVal)) return doubleVal;
                    else return null;
                }
            });

            ret.Add(new Reviver()
            {
                Revive = (type, key, value) =>
                {
                    Guid guidVal;
                    if (Guid.TryParse(value, out guidVal)) return guidVal;
                    else return null;
                }
            });

            ret.Add(new Reviver()
            {
                Revive = (type, key, value) =>
                {

                    if (value.StartsWith("/Date(") == false)
                    {
                        return null;
                    }
                    else
                    {
                        value = value.Replace("/Date(", "");
                        value = value.Replace(")/", "");
                        long ms;
                        if (long.TryParse(value, out ms))
                        {
                            return new DateTime(1970, 1, 1,0,0,0,0,DateTimeKind.Utc) + TimeSpan.FromMilliseconds(ms);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            });

            ret.Add(new Reviver()
            {
                Revive = (type, key, value) =>
                {
                    if (value.ToLower() == "true") return true;
                    else if (value.ToLower() == "false") return false;
                    else return null;
                }
            });

            return ret;
        }
    }

    public class JSONDbContext : IDisposable
    {
        string fileName;

        public int NextId { get; set; }

        public JSONDbContext() : this(null)
        {
        }

        public JSONDbContext(string fn)
        {
            fileName = fn ?? @"C:\temp\" + GetType().Name + ".js";
            NextId = 1;

            if (File.Exists(fileName))
            {
                RefreshDiscardChanges();
            }
            else
            {
                InitializeEmptySets();
            }
        }

        public void RefreshDiscardChanges()
        {
            object me = this;
            JSON.Json.Parse(ref me, File.ReadAllText(fileName), null);
        }

        public void SaveChanges()
        {
            EnsureUniqueIds();
            var text = JSON.Json.Stringify(this);
            File.WriteAllText(fileName, text);
        }

        private void InitializeEmptySets()
        {
            foreach (PropertyInfo info in GetType().GetProperties())
            {
                var val = info.GetValue(this, null);
                if(val == null)
                {
                    try
                    {
                        info.SetValue(this, Activator.CreateInstance(info.PropertyType), null);
                    }
                    catch (Exception) { }
                }
            }
        }

        private void EnsureUniqueIds()
        {
            foreach (PropertyInfo info in GetType().GetProperties())
            {
                var val = info.GetValue(this, null);
                if (val is IList == false || ((IList)val).Count == 0) continue;

                IList listVal = (IList)val;
                PropertyInfo idProp = listVal[0].GetType().GetProperty("Id");

                if (idProp == null || idProp.PropertyType != typeof(int)) continue;

                foreach (var element in listVal)
                {
                    int myId = (int)idProp.GetValue(element, null);
                    if (myId == 0)
                    {
                        idProp.SetValue(element, NextId++, null);
                    }
                }
            }
        }

        public void Dispose()
        {

        }
    }

}
