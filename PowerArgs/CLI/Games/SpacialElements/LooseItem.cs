using PowerArgs.Cli.Physics;
using System.Linq;

namespace PowerArgs.Games
{
    public abstract class LooseItem : SpacialElement
    {
        public ConsoleString DisplayString { get; set; }

        public static Event<LooseItem> OnIncorporated { get; private set; } = new Event<LooseItem>();

        public Event Incorporated { get; private set; } = new Event();
        public override void Evaluate()
        {
            var target = SpaceTime.CurrentSpaceTime.Elements
                .Where(e =>
                    e is Character &&
                    CanIncorporate(e as Character) &&
                    e.Touches(this))
                .Select(e => e as Character).FirstOrDefault();

            if (target != null)
            {
                Incorporate(target);
                OnIncorporated.Fire(this);
                this.Lifetime.Dispose();
                Incorporated?.Fire();
            }
        }

        public abstract bool CanIncorporate(Character target);
        public abstract void Incorporate(Character target);
    }
}
