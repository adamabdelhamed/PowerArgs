using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public class ConsolePixel
    {
        public ConsoleCharacter? Value { get; set; }
        public ConsoleCharacter? LastDrawnValue { get; private set; }

        public bool HasChanged
        {
            get
            {
                if(Value.HasValue == false && LastDrawnValue.HasValue == false)
                {
                    return false;
                }
                else if(LastDrawnValue.HasValue ^ Value.HasValue)
                {
                    return true;
                }
                else if(LastDrawnValue.Value.Equals(Value.Value))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public void Sync()
        {
            this.LastDrawnValue = Value;
        }
    }
}
