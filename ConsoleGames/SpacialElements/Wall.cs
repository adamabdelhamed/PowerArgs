using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace ConsoleGames
{
    public class Wall : SpacialElement, IDestructible, IObservableObject
    {
        protected ObservableObject observable;
        public bool SuppressEqualChanges { get; set; }
        public IDisposable SubscribeUnmanaged(string propertyName, Action handler) => observable.SubscribeUnmanaged(propertyName, handler);
        public void SubscribeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        public IDisposable SynchronizeUnmanaged(string propertyName, Action handler) => observable.SynchronizeUnmanaged(propertyName, handler);
        public void SynchronizeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SynchronizeForLifetime(propertyName, handler, lifetimeManager);

        public bool ForcePen { get; set; }
        public ConsoleCharacter? Pen { get; set; }

        public Event Damaged { get; private set; } = new Event();

        public Event Destroyed { get; private set; } = new Event();

        public float HealthPoints { get { return observable.Get<float>(); } set { observable.Set(value); } } 

        public Wall()
        {
            observable = new ObservableObject(this);
            HealthPoints = 20;
            ResizeTo(1, 1);

            observable.SubscribeForLifetime(nameof(HealthPoints), this.SizeOrPositionChanged.Fire, this.Lifetime);
        }
    }

    [SpacialElementBinding(typeof(Wall))]
    public class WallRenderer : ThemeAwareSpacialElementRenderer
    {
        public ConsoleCharacter Style { get; set; } = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.White);

        protected override void OnPaint(ConsoleBitmap context)
        {
            var w = Element as Wall;
            if(w.Pen.HasValue && w.Pen.Value.Value != ' ')
            {
                context.Pen = w.Pen.Value;
            }
            else
            {
                context.Pen = Style;
            }

            if (context.Pen.Value == ' ' && context.Pen.BackgroundColor == ConsoleString.DefaultBackgroundColor)
            {
                context.Pen = new ConsoleCharacter('W');
            }

            if ((Element as Wall).ForcePen)
            {
                context.Pen = (Element as Wall).Pen.Value;
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
