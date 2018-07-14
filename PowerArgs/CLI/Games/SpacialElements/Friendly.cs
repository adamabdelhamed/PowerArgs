using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public class Friendly : Character
    {
        public Friendly()
        {
            this.HealthPoints = 1;
        }
    }

    [SpacialElementBinding(typeof(Friendly))]
    public class FriendlyRenderer : ThemeAwareSpacialElementRenderer
    {
        public FriendlyRenderer()
        {
            this.TransparentBackground = true;
            CanFocus = false;
            ZIndex = 10;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter('F', ConsoleColor.Green);
            if ((Element as Character).Symbol.HasValue)
            {
                context.Pen = new ConsoleCharacter((Element as Character).Symbol.Value, context.Pen.ForegroundColor, context.Pen.BackgroundColor);
            }

            context.FillRect(0, 0, Width, Height);
        }
    }

    public class FriendlyReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
            if (item.HasSimpleTag("friendly") == false)
            {
                hydratedElement = null;
                return false;
            }

            var friendly = new Friendly();
            friendly.Inventory.Items.Add(new Pistol() { AmmoAmount = 100, HealthPoints = 10, });
            hydratedElement = friendly;
            new Bot(friendly, new List<IBotStrategy> {  new AvoidEnemies() });
            return true;
        }
    }
}
