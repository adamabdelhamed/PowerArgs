using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class Door : Wall
    {
        private Rectangle closedBounds;
        public Location OpenLocation { get; private set; }

        public DoorThreshold Threshold { get; private set; }

        public bool IsOpen
        {
            get
            {
                return Bounds.Location.Equals(OpenLocation);
            }
            set
            {
                if(value && IsOpen)
                {
                    return;
                }
                else if(value)
                {
                    this.Bounds.MoveTo(OpenLocation);
                }
                else if(value == false && IsOpen == false)
                {
                    return;
                }
                else if(value == false)
                {
                    this.Bounds.MoveTo(closedBounds.Location);
                }
            }
        }

        public Door(Rectangle closedBounds, Location openLocation) 
        {
            Initialize(closedBounds, openLocation);
        }

        public Door()
        {

        }

        public void Initialize(Rectangle closedBounds, Location openLocation)
        {
            if(this.closedBounds != null)
            {
                throw new InvalidOperationException("Already initialized");
            }

            this.Bounds = closedBounds.Clone();
            this.closedBounds = closedBounds.Clone();
            this.OpenLocation = openLocation;
            this.Threshold = new DoorThreshold(this) { Bounds = this.closedBounds.Clone() };
            this.Added.SubscribeForLifetime(() =>
            {
                Realm.Add(Threshold);
            }, this.LifetimeManager);
            this.Removed.SubscribeForLifetime(() =>
            {
                Realm.Remove(Threshold);
            }, this.LifetimeManager);
        }
    }

    public class DoorThreshold : Thing
    {
        public Door Door { get; private set; }
        public DoorThreshold(Door door)
        {
            this.Door = door;
        }
    }

    [ThingBinding(typeof(Door))]
    public class DoorRenderer : ThingRenderer
    {
        public DoorRenderer()
        {
            Background = ConsoleColor.Cyan;
        }
    }

    [ThingBinding(typeof(DoorThreshold))]
    public class DoorThresholdRenderer : ThingRenderer
    {
        public DoorThresholdRenderer()
        {
            TransparentBackground = true;
        }
    }
}
