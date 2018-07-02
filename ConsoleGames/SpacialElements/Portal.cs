using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
namespace ConsoleGames
{
    public class Portal : SpacialElement, IGameAppAware
    {
        public GameApp GameApp { get; set; }

        public Portal()
        {
            this.ResizeTo(1, 1);
        }

        public override void Evaluate() => SpaceTime.CurrentSpaceTime.Elements
            .Where(c => c is Character && c.Touches(this))
            .Select(c => c as Character)
            .ForEach(c => OnTouchedByCharacter(c));

        public string Destination { get; set; }
 
        private void OnTouchedByCharacter(Character c)
        {
            if (c == MainCharacter.Current)
            {
                var level = LevelEditor.LoadBySimpleName(Destination);
                GameApp.Load(level);
            }
        }
    }

    [SpacialElementBinding(typeof(Portal))]
    public class PortalRenderer : SpacialElementRenderer
    {
        public PortalRenderer()
        {
            Background = ConsoleColor.Magenta;
        }
    }

    public class PortalReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out SpacialElement hydratedElement)
        {
            if (item.HasValueTag("destination") == false)
            {
                hydratedElement = null;
                return false;
            }

            hydratedElement = new Portal() { Destination = item.GetTagValue("destination") };
            return true;
        }
    }
}
