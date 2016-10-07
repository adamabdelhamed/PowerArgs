using System;

namespace PowerArgs.Cli.Physics
{
    public class OneTimeInteraction : Interaction
    {
        Action action;
        public OneTimeInteraction(Action action)
        {
            this.action = action;
        }

        public override void Initialize(Scene scene)
        {
            action.Invoke();
            scene.Remove(this);
        }
    }
}
