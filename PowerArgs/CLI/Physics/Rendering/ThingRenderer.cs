using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class ThingRenderer: ConsoleControl
    {
        public Thing Thing { get; set; }

        public ThingRenderer()
        {
            Background = ConsoleColor.DarkGreen;
        }
    }
}
