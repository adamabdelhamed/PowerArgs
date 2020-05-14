using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PowerArgs.Games
{
    public class Wall : SpacialElement, IObservableObject
    {
        protected ObservableObject observable;
        public bool SuppressEqualChanges { get; set; }
        public object GetPrevious(string name) => observable.GetPrevious<object>(name);
        public IDisposable SubscribeUnmanaged(string propertyName, Action handler) => observable.SubscribeUnmanaged(propertyName, handler);
        public void SubscribeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        public IDisposable SynchronizeUnmanaged(string propertyName, Action handler) => observable.SynchronizeUnmanaged(propertyName, handler);
        public void SynchronizeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SynchronizeForLifetime(propertyName, handler, lifetimeManager);
        public T Get<T>([CallerMemberName]string name = null) => observable.Get<T>(name);
        public void Set<T>(T value, [CallerMemberName]string name = null) => observable.Set<T>(value);
        public Lifetime GetPropertyValueLifetime(string propertyName) => observable.GetPropertyValueLifetime(propertyName);

        public ConsoleCharacter? Pen { get; set; }

        public Wall()
        {
            observable = new ObservableObject(this);
            ResizeTo(1, 1);
        }
    }

    [SpacialElementBinding(typeof(Wall))]
    public class WallRenderer : SpacialElementRenderer
    {
        public ConsoleCharacter Style { get; set; } = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.White);

        protected override void OnPaint(ConsoleBitmap context)
        {
            var w = Element as Wall;
            if(w.Pen.HasValue)
            {
                context.Pen = w.Pen.Value;
            }
            else
            {
                context.Pen = Style;
            }
 
            context.FillRect(0, 0, Width, Height);
        }
    }

    public class WallReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
            hydratedElement = new Wall() { Pen = new ConsoleCharacter(item.Symbol, item.FG, item.BG) };
            return true;
        }
    }
}
