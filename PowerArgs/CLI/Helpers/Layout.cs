using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public static class Layout
    {
        public static void CenterVertically(Rectangular parent, Rectangular child)
        {
            var gap = parent.Height - child.Height;
            var y = gap / 2;
            child.Y = y;
        }

        public static void CenterHorizontally(Rectangular parent, Rectangular child)
        {
            var gap = parent.Width - child.Width;
            var x = gap / 2;
            child.X = x;
        }
    }
}
