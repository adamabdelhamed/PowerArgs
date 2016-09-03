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

        public static Thickness Parse(string s)
        {
            var split = s.Split(',');

            int l = split.Length > 0 ? int.Parse(split[0]) : 0;
            int r = split.Length > 1 ? int.Parse(split[1]) : 0;
            int t = split.Length > 2 ? int.Parse(split[2]) : 0;
            int b = split.Length > 3 ? int.Parse(split[3]) : 0;

            return new Thickness(1, r, t, b);
        }
    }
}
