using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs
{
    public static class DictionaryEx
    {
        public static void Set<TK,TV>(this Dictionary<TK,TV> dictionary, TK key, TV value)
        {
            if(dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }
    }
}
