using System;

namespace PowerArgs.Cli.Physics
{
    public class Gravity : Force
    {
        public Gravity(Velocity tr) : base(tr, 32, 90, TimeSpan.FromSeconds(-1)) { }
    }
}
