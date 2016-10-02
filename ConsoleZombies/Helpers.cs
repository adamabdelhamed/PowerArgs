using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public static class Helpers
    {
        public static void RoundToNearestPixel(this Thing t)
        {
            var padding = (float)((Math.Ceiling(t.Bounds.W) - t.Bounds.W) / 2);
            t.Bounds.MoveTo(new Location()
            {
                X = (float)Math.Round(t.Bounds.X),
                Y = (float)Math.Round(t.Bounds.Y)
            });
            t.Bounds.PadLocation(padding);
        }
    }
}
