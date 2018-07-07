using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System.Collections.Generic;

namespace ConsoleGames
{
    public class Trigger : SpacialElement
    {
        public Event<string> Fired { get; private set; } = new Event<string>();
        public string Id { get; set; }
        public float Range { get; set; }

        public override void Evaluate()
        {
            if(MainCharacter.Current == null)
            {
                return;
            }

            var angle = MainCharacter.Current.CalculateAngleTo(this);
            var normalizedRange = SpaceExtensions.NormalizeQuantity(Range, angle);
            if(this.CalculateDistanceTo(MainCharacter.Current) <= normalizedRange)
            {
                Fired.Fire(Id);
                Lifetime.Dispose();
            }
        }
    }

    [SpacialElementBinding(typeof(Trigger))]
    public class TriggerRenderer : SpacialElementRenderer
    {
        public TriggerRenderer()
        {
            IsVisible = false;
        }
    }

    public class TriggerReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
            if (item.HasValueTag("trigger") == false)
            {
                hydratedElement = null;
                return false;
            }

            var range = item.HasValueTag("range") && float.TryParse(item.GetTagValue("range"), out float result) ? result : 5f;
            var trigger = new Trigger() { Id = item.GetTagValue("trigger"), Range = range };
            hydratedElement = trigger;
            return true;
        }
    }
}
