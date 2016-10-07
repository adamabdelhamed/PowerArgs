using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public abstract class Ammo : Item
    {
        public abstract ConsoleCharacter Symbol { get;}
        public int Amount { get; set; }
    }

    [ThingBinding(typeof(Ammo))]
    public class AmmoRenderer : ThingRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = (Thing as Ammo).Symbol;
            context.FillRect(0, 0, Width, Height);
        }
    }
}
