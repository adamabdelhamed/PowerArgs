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
                    return;
                }
                else if(value)
                {
                    SoundEffects.Instance.PlaySound("opendoor");
                    this.Bounds.MoveTo(OpenLocation);
                }
                else if(value == false && IsOpen == false)
                {
                    return;
                }
                else if(value == false)
                {
                    SoundEffects.Instance.PlaySound("closedoor");
                    this.Bounds.MoveTo(ClosedBounds.Location);
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
            if(this.ClosedBounds != null)
            {
                throw new InvalidOperationException("Already initialized");
            }

            this.Bounds = closedBounds.Clone();
            this.ClosedBounds = closedBounds.Clone();
            this.OpenLocation = openLocation;
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
