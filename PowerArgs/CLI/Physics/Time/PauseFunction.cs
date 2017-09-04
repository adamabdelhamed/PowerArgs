using System;
using System.Threading;

namespace PowerArgs.Cli.Physics
{
    /// <summary>
    /// A function used to temporarily pause a time simulation
    /// </summary>
    public class PauseFunction : TimeFunction
    {
        /// <summary>
        /// This method sleeps until the function is removed from the time simulation
        /// </summary>
        public override void Initialize()
        {
            while (Lifetime.IsExpired == false)
            {
                Thread.Sleep(0);
            }
        }

        /// <summary>
        /// Not implemented, pause is impemented by the Initialize method.
        /// </summary>
        public override void Evaluate() => throw new NotImplementedException();
    }
}