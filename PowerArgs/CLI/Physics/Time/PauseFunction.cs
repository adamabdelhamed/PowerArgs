using System;
using System.Threading;

namespace PowerArgs.Cli.Physics
{
    public class PauseFunction : TimeFunction
    {
        public override void Initialize()
        {
            while (Lifetime.IsExpired == false)
            {
                Thread.Sleep(0);
            }
        }

        public override void Evaluate() => throw new NotImplementedException();
    }
}