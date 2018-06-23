using PowerArgs.Cli.Physics;
using System.Linq;

namespace ConsoleGames
{
    public abstract class LooseItem : SpacialElement
    {
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
                this.Lifetime.Dispose();
            }
        }

        public abstract bool CanIncorporate(Character target);
        public abstract void Incorporate(Character target);
    }
}
