using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Text;

namespace PowerArgs.Games
{
    public abstract class MultiPlayerContest : Lifetime
    {
        public abstract Promise Start(MultiPlayerServer server);
    }
}
