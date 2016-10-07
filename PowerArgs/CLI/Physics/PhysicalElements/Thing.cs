using System;

namespace PowerArgs.Cli.Physics
{
    public class Thing : Lifetime, IObservableObject
    {
        private ObservableObject observable = new ObservableObject();

        protected ObservableObject Observable
        {
            get
            {
                return observable;
            }
        }

        public Event Added { get; private set; } = new Event();
        public Event Removed { get; private set; } = new Event();
        public Event Updated { get; private set; } = new Event();

        public long Id { get; internal set; }
        public Rectangle Bounds { get; set; }
        public Scene Scene { get; internal set; }

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

        public bool SuppressEqualChanges
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Thing()
        {
            Governor = new RateGovernor();
            Bounds = new Rectangle(0,0,0,0);
        }
        public Thing(Rectangle bounds) : this()
        {
            this.Bounds = bounds;
        }

        public Thing(float x, float y, float w, float h) : this(new Rectangle(x, y, w, h))
        {
        }

        public virtual void InitializeThing(Scene r)
        {
        }

        public virtual void Behave(Scene r)
        {
        }

        public PropertyChangedSubscription SubscribeUnmanaged(string propertyName, Action handler)
        {
            return observable.SubscribeUnmanaged(propertyName, handler);
        }

        public void SubscribeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager)
        {
            observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        }

        public PropertyChangedSubscription SynchronizeUnmanaged(string propertyName, Action handler)
        {
            return observable.SynchronizeUnmanaged(propertyName, handler);
        }

        public void SynchronizeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager)
        {
            observable.SynchronizeForLifetime(propertyName, handler, lifetimeManager);
        }
    }
}
