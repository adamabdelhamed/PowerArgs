using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{


    public class Character : SpacialElement, IObservableObject
    {
        public bool IsVisible { get; set; } = true;
        public MultiPlayerClient MultiPlayerClient { get; set; }
        public char? Symbol { get; set; }
        public Inventory Inventory { get => observable.Get<Inventory>(); set => observable.Set(value); }  


        protected ObservableObject observable;
        public bool SuppressEqualChanges { get; set; }
        public object GetPrevious(string name) => observable.GetPrevious<object>(name);
        public IDisposable SubscribeUnmanaged(string propertyName, Action handler) => observable.SubscribeUnmanaged(propertyName, handler);
        public void SubscribeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        public IDisposable SynchronizeUnmanaged(string propertyName, Action handler) => observable.SynchronizeUnmanaged(propertyName, handler);
        public void SynchronizeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SynchronizeForLifetime(propertyName, handler, lifetimeManager);
        public SpacialElement Target { get; set; }

        public SpeedTracker Speed { get; set; }

        public Character()
        {
            observable = new ObservableObject(this);
            this.SubscribeForLifetime(nameof(Inventory), () => this.Inventory.Owner = this, this.Lifetime);
            Inventory = new Inventory();
            Speed = new SpeedTracker(this) { Bounciness = 0 };
            this.ResizeTo(1, 1);
        }
    }
}
