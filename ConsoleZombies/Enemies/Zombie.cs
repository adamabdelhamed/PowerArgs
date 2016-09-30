using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace ConsoleZombies
{
    public class Zombie : Thing
    {
        public Zombie()
        {
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(0, 0, 1, 1);
        }

        public override void InitializeThing(Realm r)
        {
            new Seeker(this, MainCharacter.Current, 1);
        }
    }

    [ThingBinding(typeof(Zombie))]
    public class ZombieRenderer : ThingRenderer
    {
        public ZombieRenderer()
        {
            this.TransparentBackground = true;
            CanFocus = true;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if (HasFocus)
            {
                context.Pen = new PowerArgs.ConsoleCharacter('Z', GameTheme.DefaultTheme.FocusColor, ConsoleColor.Gray);
            }
            else
            {
                context.Pen = new PowerArgs.ConsoleCharacter('Z', ConsoleColor.DarkRed, ConsoleColor.Gray);
            }
            context.DrawPoint(0, 0);
        }
    }
}
