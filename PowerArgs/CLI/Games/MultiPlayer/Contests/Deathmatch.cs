using System;
using System.Collections.Generic;
using System.Text;

namespace PowerArgs.Games
{
    public class Deathmatch : MultiPlayerContest
    {
        public override Promise Start(MultiPlayerServer server)
        {
            var d = Deferred.Create();
            return d.Promise;
        }
    }
}
