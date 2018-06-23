using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleGames.Shooter 
{
    public class RemoteMine : Explosive
    {
        public RemoteMine(float x, float y, float angleIcrement, float range) : base(x,y, angleIcrement, range)
        {

        }

        public void Detonate()
        {
            Explode();
        }
    }
}
