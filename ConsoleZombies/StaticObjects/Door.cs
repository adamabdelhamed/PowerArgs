using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleZombies
{
    public class Door : Wall
    {
        public Rectangle ClosedBounds { get; private set; }
        public Location OpenLocation { get; private set; }
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
                    FindCieling().ForEach(c => c.IsVisible = false);
                }
                else if(value)
                {
                    SoundEffects.Instance.PlaySound("opendoor");
                    this.Bounds.MoveTo(OpenLocation);
                    FindCieling().ForEach(c => c.IsVisible = false);
                }
                else if(value == false && IsOpen == false)
                {
                    FindCieling().ForEach(c => c.IsVisible = true);
                }
                else if(value == false)
                {
                    SoundEffects.Instance.PlaySound("closedoor");
                    this.Bounds.MoveTo(ClosedBounds.Location);
                    FindCieling().ForEach(c => c.IsVisible = true);
                }
            }
        }

        public Door(Rectangle closedBounds, Location openLocation)  : this()
        {
            Initialize(closedBounds, openLocation);
        }

        public Door()
        {
            Added.SubscribeForLifetime(() => 
            {
                this.IsOpen = this.IsOpen;
            }, this.LifetimeManager);
            Removed.SubscribeForLifetime(() => FindCieling().ForEach(c => Scene.Remove(c)), this.LifetimeManager);
        }

        public List<Ceiling> FindCieling()
        {
            List<Ceiling> ret = new List<Ceiling>();
            if(Scene == null)
            {
                return ret;
            }
            foreach(var cieling in Scene.Things.Where(t => t is Ceiling).Select(t => t as Ceiling).OrderBy(c => c.Bounds.Location.CalculateDistanceTo(this.Bounds.Location)))
            {
                if(cieling.Bounds.Location.CalculateDistanceTo(this.Bounds.Location) <= 1.25)
                {
                    ret.Add(cieling);
                }
                else
                {
                    foreach(var alreadyAdded in ret.ToArray())
                    {
                        if (cieling.Bounds.Location.CalculateDistanceTo(alreadyAdded.Bounds.Location) <= 1)
                        {
                            ret.Add(cieling);
                            break;
                        }
                    }
                }
            }

            return ret;
        }

        public void Initialize(Rectangle closedBounds, Location openLocation)
        {
            if(this.ClosedBounds != null)
            {
                throw new InvalidOperationException("Already initialized");
            }

            this.Bounds = closedBounds.Clone();
            this.ClosedBounds = closedBounds.Clone();
            this.OpenLocation = openLocation;
            this.IsOpen = false;
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
}
