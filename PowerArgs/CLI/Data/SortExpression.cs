using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class SortExpression
    {
        public string Value { get; set; }
        public bool Descending { get; set; }

        public SortExpression(string value, bool descending = false)
        {
            this.Value = value;
            this.Descending = descending;
        }
    }
}
