using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace ConsoleZombies
{
    public class Wall : Thing, IDestructible
    {
        public float HealthPoints { get; set; }

        public Wall()
        {
            HealthPoints = 20;
        }
    }

    [ThingBinding(typeof(Wall))]
    public class WallRenderer : ThingRenderer
    {
        public WallRenderer()
        {
            this.Background = new GameTheme().WallColor;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if(HasFocus)
            {
                context.Pen = new PowerArgs.ConsoleCharacter(' ', GameTheme.DefaultTheme.FocusColor);
                context.FillRect(0, 0, Width, Height);
            }
        }
    }
}
