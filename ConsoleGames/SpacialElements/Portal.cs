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

        public string LevelId { get; set; }

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
            if (c == MainCharacter.Current && LevelId != null)
            {
                GameApp.Load(LevelId);
            }
        }
    }

    [SpacialElementBinding(typeof(Portal))]
    public class PortalRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Magenta);

        protected override void OnPaint(ConsoleBitmap context)
        {
            if((Element as Portal).LevelId == null)
            {
                Style = new ConsoleCharacter('?', ConsoleColor.Black, ConsoleColor.Magenta);
            }
            base.OnPaint(context);
        }
    }

    public class PortalReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
            if (item.HasValueTag("destination") == false)
            {
                hydratedElement = null;
                return false;
            }

            hydratedElement = new Portal();

            try
            {
                (hydratedElement as Portal).LevelId = item.GetTagValue("destination");
            }
            catch(Exception ex)
            {

            }
            return true;
        }
    }
}
