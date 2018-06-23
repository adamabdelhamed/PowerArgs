using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public class Wall : SpacialElement, IDestructible, IObservableObject
    {
        protected ObservableObject observable;
        public bool SuppressEqualChanges { get; set; }
        public IDisposable SubscribeUnmanaged(string propertyName, Action handler) => observable.SubscribeUnmanaged(propertyName, handler);
        public void SubscribeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager) => observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        public IDisposable SynchronizeUnmanaged(string propertyName, Action handler) => observable.SynchronizeUnmanaged(propertyName, handler);
        public void SynchronizeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager) => observable.SynchronizeForLifetime(propertyName, handler, lifetimeManager);

        public Event Damaged { get; private set; } = new Event();

        public Event Destroyed { get; private set; } = new Event();

        public float HealthPoints { get { return observable.Get<float>(); } set { observable.Set(value); } } 

        public Wall()
        {
            observable = new ObservableObject(this);
            HealthPoints = 20;
            ResizeTo(1, 1);

            observable.SubscribeForLifetime(nameof(HealthPoints), this.SizeOrPositionChanged.Fire, this.Lifetime.LifetimeManager);
        }
    }

    [SpacialElementBinding(typeof(Wall))]
    public class WallRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            if((Element as Wall).HealthPoints < 10)
            {
                context.Pen = new PowerArgs.ConsoleCharacter(' ', backgroundColor: System.ConsoleColor.DarkGray);
            }
            else
            {
                context.Pen = new PowerArgs.ConsoleCharacter(' ', backgroundColor: System.ConsoleColor.Gray);
            }

            context.DrawPoint(0, 0);
        }
    }
}
