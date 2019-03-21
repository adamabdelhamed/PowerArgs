using PowerArgs.Cli.Physics;

namespace PowerArgs.Games
{
    public class Bot : SpacialElementFunction
    {
        private Character me;
        public IBotStrategy Strategy { get; set; }
        public Bot(Character toAnimate) : base(toAnimate)
        {
            this.me = toAnimate;
        }

        public override void Initialize()
        {
            if (Strategy != null)
            {
                Strategy.Me = me;
            }
        }

        public override void Evaluate()
        {
            if (Strategy != null)
            {
                Strategy.Me = me;
            }
            Strategy?.Work();
        }
    }
}
