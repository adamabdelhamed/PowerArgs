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
            RenderLoop.Current.QueueAction(() =>
            {
                if(target.IsExpired)
                {
                    return;
                }

                if (target.Realm != null)
                {
                    target.Realm.Add(this);
                }
                else
                {
                    target.Added.SubscribeForLifetime(() =>
                    {
                        target.Realm.Add(this);
                    }, target.LifetimeManager);
                }


                MyThing.Removed.SubscribeForLifetime(() =>
                {
                    target.Realm.Remove(this);
                }, target.LifetimeManager);
            });
        }
    }
}
