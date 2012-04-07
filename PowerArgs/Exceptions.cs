using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class ArgException : Exception
    {
        public ArgException(string msg) : base(msg) { }
        public ArgException(string msg, Exception inner) : base(msg, inner) { }
    }

    public class InvalidArgDefinitionException : Exception
    {
        public InvalidArgDefinitionException(string msg) : base(msg) { }
        public InvalidArgDefinitionException(string msg, Exception inner) : base(msg, inner) { }
    }
}
