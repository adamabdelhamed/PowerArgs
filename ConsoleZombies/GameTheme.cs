using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class GameTheme : Theme
    {
        public ConsoleColor WallColor { get; set; } = ConsoleColor.DarkGray;
        public GameTheme() : base()
        {

        }
    }
}
