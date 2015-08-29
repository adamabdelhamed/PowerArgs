using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public struct Size
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Size(int w, int h) : this()
        {
            Width = w;
            Height = h;
        }
    }
}
