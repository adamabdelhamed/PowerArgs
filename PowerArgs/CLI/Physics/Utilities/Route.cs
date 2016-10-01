using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class Route
    {
        public List<Location> Steps { get; private set; } = new List<Location>();
        public List<Thing> Obstacles { get; private set; } = new List<Thing>();
    }
}
