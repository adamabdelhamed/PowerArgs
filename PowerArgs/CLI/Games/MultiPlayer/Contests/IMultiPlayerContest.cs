using System;
using System.Collections.Generic;
using System.Text;

namespace PowerArgs.Games
{
    public interface IMultiPlayerContest
    {
        MultiPlayerServer Server { get; set; }
        Promise<MultiPlayerMessage> GetResponse(MultiPlayerMessage request);
    }
}
