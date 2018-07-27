using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Games
{
    public class MultiPlayerContestOptions
    {
        public int MaxPlayers { get; set; }
        public MultiPlayerServer Server { get; set; }
    }

    public abstract class MultiPlayerContest<T> : Lifetime where T : MultiPlayerContestOptions
    {
        public T Options { get; private set; }

        public MultiPlayerServer Server => Options.Server;
        public string ServerId => Server.ServerId;

        public MultiPlayerContest(T options)
        {
            this.Options = options;
        }
    }
}
