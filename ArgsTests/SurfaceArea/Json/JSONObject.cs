using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Dynamic;

namespace JSON
{
    public class JSONObject : DynamicObject, IEnumerable
    {
        public enum JSONType
        {
            Simple, List, Object
        }

        public Dictionary<object, JSONObject> Properties { get; private set; }
        public List<JSONObject> Elements { get; private set; }
        public object Value { get; set; }

        public JSONType Type { get; private set; }

        public int Count
        {
            get
            {
                if (Type == JSONType.List)
                {
                    return Elements.Count;
                }
                else
                {
                    return Properties.Count;
                }
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public string Stringify()
        {
            return Json.Stringify(this);
        }

        public JSONObject(JSONType type = JSONType.Simple)
        {
            this.Type = type;
            Elements = new List<JSONObject>();
            Properties = new Dictionary<object, JSONObject>();
        }

        public dynamic this[object index]
        {
            get
            {
                if (Type == JSONType.List && index is int)
                {
                    var item = Elements[(int)index];

                    if (item.Type == JSONType.Simple)
                    {
                        return item.Value;
                    }
                    else
                    {
                        return item;
                    }
                }
                else if (Properties.ContainsKey(index))
                {
                    if (Type == JSONType.Object)
                    {
                        return Properties[index];
                    }
                    else
                    {
                        return Properties[index].Value;
                    }
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (Type == JSONType.List)
                {
                    if (value is JSONObject)
                    {
                        Elements[(int)index] = value;
                    }
                    else if (Elements[(int)index].Type == JSONType.Simple)
                    {
                        Elements[(int)index].Value = value;
                    }
                    else
                    { 
                         throw new NotSupportedException("Cannot assign the specified value.  The value must either be a JSONObject or a simple type matching a simple array");
                    }
                }
                else if (Type == JSONType.Object)
                {
                    if (value is JSONObject)
                    {
                        if (Properties.ContainsKey(index))
                        {
                            Properties[index] = value;
                        }
                        else
                        {
                            Properties.Add(index, value);
                        }
                    }
                    else if (Properties.ContainsKey(index))
                    {
                        Properties[index].Value = value;
                    }
                    else
                    {
                        Properties.Add(index, new JSONObject() { Value = Value });
                    }
                }
                else
                {
                    Value = value;
                }
            }
        }

        public void Add(object o)
        {
            if (Type != JSONType.List) throw new NotSupportedException("This is not a collection.  Cannot add.");
            if (o is JSONObject)
            {
                Elements.Add((JSONObject)o);
            }
            else
            {
                Elements.Add(new JSONObject(JSONType.Simple) { Value = o });
            }
        }

        public void AddRange(IEnumerable o)
        {
            foreach(object obj in o) Add(obj);
        }

        public bool Remove(object o)
        {
            if (Type != JSONType.List) throw new NotSupportedException("This is not a collection.  Cannot add.");
            if (o is JSONObject)
            {
                return Elements.Remove((JSONObject)o);
            }
            else
            {
                var toRemove = (from e in Elements where e.Value != null && e.Value.Equals(o) select e).FirstOrDefault();
                if (toRemove != null)
                {
                    return Elements.Remove(toRemove);
                }
                else
                {
                    return false;
                }
            }
        }

        public void RemoveRange(IEnumerable o)
        {
            foreach (object obj in o) Remove(obj);
        }

        public void RemoveAt(int i)
        {
            Elements.RemoveAt(i);
        }

        public bool Contains(object o)
        {
            if (o is JSONObject) throw new NotSupportedException("Cannot test for containing a JSONObject");
            return (from e in Elements where e.Value != null && e.Value.Equals(o) select e).Count() > 0;
        }

        public int IndexOf(object o)
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i].Value != null && Elements[i].Value.Equals(o))
                {
                    return i;
                }
            }
            return -1;
        }

        public T ValueOf<T>(object index)
        {
            var ret = this[index];
            if (ret == null) return default(T);
            return (T)ret.Value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (Type == JSONType.List)
            {
                result = null;
                return false;
            }
            else
            {
                if (Properties.ContainsKey(binder.Name))
                {
                    var ret = this[binder.Name];
                    if (ret is JSONObject == false || ret.Value == null) result = ret;
                    else result = ret.Value;
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            if (Type == JSONType.List)
            {

                if (Elements.Count > 0 && Elements[0].Type == JSONType.Simple)
                {
                    return (from e in Elements select e.Value).GetEnumerator();
                }
                else
                {
                    return Elements.GetEnumerator();
                }
            }
            else
            {
                return Properties.Keys.GetEnumerator();
            }
        }
    }
}
