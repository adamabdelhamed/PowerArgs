using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;
namespace ConsoleGames
{
    public class Portal : SpacialElement, IGameAppAware
    {
        public GameApp GameApp { get; set; }

        public Event<Character> TouchedByCharacter { get; private set; } = new Event<Character>();

        public Portal()
        {
            this.ResizeTo(1, 1);
        }

        public override void Evaluate() => SpaceTime.CurrentSpaceTime.Elements
            .Where(c => c is Character && c.Touches(this))
            .Select(c => c as Character)
            .ForEach(c => TouchedByCharacter.Fire(c));
        
    }

    [SpacialElementBinding(typeof(Portal))]
    public class PortalRenderer : SpacialElementRenderer
    {
        public PortalRenderer()
        {
            Background = ConsoleColor.Magenta;
        }
    }
}
