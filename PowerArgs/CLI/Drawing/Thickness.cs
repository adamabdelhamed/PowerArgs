using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public struct Thickness
    { 
        public int Left { get; set; }
        public int Right { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }

        public Thickness(int l, int r, int t, int b)
        {
            this.Left = l;
            this.Right = r;
            this.Top = t;
            this.Bottom = b;
        }
    }
}
