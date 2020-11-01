using System;
using System.Collections.Generic;
using System.Text;

namespace PowerArgs 
{
    public static class ExceptionsEx
    {
        public static List<Exception> Clean(this Exception ex)
        {
            if (ex is AggregateException)
            {
                return Clean(((AggregateException)ex).InnerExceptions);
            }
            else
            {
                return new List<Exception>() { ex };
            }
        }


        public static List<Exception> Clean(this IEnumerable<Exception> inners)
        {
            List<Exception> cleaned = new List<Exception>();
            foreach (var exception in inners)
            {
                if (exception is AggregateException)
                {
                    cleaned.AddRange(Clean(((AggregateException)exception).InnerExceptions));
                }
                else
                {
                    cleaned.Add(exception);
                }
            }

            return cleaned;
        }
    }
}
