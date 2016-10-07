using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class ThingInteraction : Interaction
    {
        public Thing MyThing { get; set; }
        public ThingInteraction() { }
        public ThingInteraction(Thing target)
        {
            this.MyThing = target;
            Scene.Current.QueueAction(() =>
            {
                if(target.IsExpired)
                {
                    return;
                }

                if (target.Scene != null)
                {
                    target.Scene.Add(this);
                }
                else
                {
                    target.Added.SubscribeForLifetime(() =>
                    {
                        target.Scene.Add(this);
                    }, target.LifetimeManager);
                }


                MyThing.Removed.SubscribeForLifetime(() =>
                {
                    target.Scene.Remove(this);
                }, target.LifetimeManager);
            });
        }
    }
}
