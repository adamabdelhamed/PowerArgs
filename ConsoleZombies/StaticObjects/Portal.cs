using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class Portal : Thing
    {
        public Event PortalEntered { get; private set; } = new Event();
        public string DestinationId { get; set; }

        public Portal()
        {
            this.Governor.Rate = TimeSpan.FromSeconds(.2);
        }

        public override void Behave(Scene r)
        {
            if(MainCharacter.Current == null)
            {
                return;
            }
            else if(this.Bounds.Hits(MainCharacter.Current.Bounds))
            {
                PortalEntered.Fire();
            }
        }
    }

    [ThingBinding(typeof(Portal))]
    public class PortalRenderer : ThingRenderer
    {
        public PortalRenderer()
        {
            this.Background = ConsoleColor.Magenta;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new PowerArgs.ConsoleCharacter('O', ConsoleColor.White, ConsoleColor.Magenta);
            context.FillRect(0, 0, Width, Height);
        }
    }
}
