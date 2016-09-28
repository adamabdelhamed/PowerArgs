using System;

namespace PowerArgs.Cli.Physics
{
    public class Thing : Lifetime
    {
        public Event Added { get; private set; } = new Event();
        public Event Removed { get; private set; } = new Event();
        public Event Updated { get; private set; } = new Event();

        public long Id { get; internal set; }
        public Rectangle Bounds { get; set; }
        public Realm Realm { get; internal set; }

        public RateGovernor Governor { get; private set; }
        public TimeSpan LastBehavior { get; internal set; }

        public float Left
        {
            get
            {
                return Bounds.Location.X;
            }
        }

        public float Top
        {
            get
            {
                return Bounds.Location.Y;
            }
        }

        public float Right
        {
            get
            {
                return Bounds.Location.X + Bounds.Size.W;
            }
        }

        public float Bottom
        {
            get
            {
                return Bounds.Location.Y + Bounds.Size.H;
            }
        }

        public Thing()
        {
            Governor = new RateGovernor();
        }
        public Thing(Rectangle bounds) : this()
        {
            this.Bounds = bounds;
        }

        public Thing(float x, float y, float w, float h) : this(new Rectangle(x, y, w, h))
        {
        }

        public virtual void InitializeThing(Realm r)
        {
        }

        public virtual void Behave(Realm r)
        {
        }
    }
}
