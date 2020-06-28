using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PowerArgs.Games
{
    public class Ceiling : SpacialElement, IObservableObject
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

        public bool IsVisible { get => observable.Get<bool>(); set => observable.Set(value); }
        public Ceiling()
        {
            observable = new ObservableObject(this);
            IsVisible = true;
            this.SubscribeForLifetime(nameof(IsVisible), this.SizeOrPositionChanged.Fire, this.Lifetime);
            AddTag(SpacialAwareness.PassThruTag);
        }

        public void Seed()
        {
            var walls = SpaceTime.CurrentSpaceTime.Elements.WhereAs<Wall>().ToList();
            var done = false;
            for (var y = Top; y < SpaceTime.CurrentSpaceTime.Height; y++)
            {
                for (var x = Left; x < SpaceTime.CurrentSpaceTime.Width; x++)
                {
                    if (x == Left && y == Top) continue;

                    var canPlaceCeiling = walls.Where(w => w.Touches(PowerArgs.Cli.Physics.RectangularF.Create(x, y, 1, 1))).Count() == 0;

                    if (canPlaceCeiling == false)
                    {
                        if (y > Top && x == Left)
                        {
                            done = true;
                        }
                        break;
                    }
                    else
                    {
                        var c = SpaceTime.CurrentSpaceTime.Add(new Ceiling());
                        c.MoveTo(x, y);
                    }
                }

                if (done)
                {
                    break;
                }
            }
        }

        public void SeedRectangular()
        {
            var walls = SpaceTime.CurrentSpaceTime.Elements.WhereAs<Wall>().ToList();
            var done = false;
            var w = 1;
            var h = 0;
            for (var y = Top; y < SpaceTime.CurrentSpaceTime.Height; y++)
            {
                h++;
                for (var x = Left; x < SpaceTime.CurrentSpaceTime.Width; x++)
                {
                    if (x == Left && y == Top) continue;

                    var canPlaceCeiling = walls.Where(wall => wall.Touches(PowerArgs.Cli.Physics.RectangularF.Create(x, y, 1, 1))).Count() == 0;

                    if (canPlaceCeiling == false)
                    {
                        if (y > Top && x == Left)
                        {
                            done = true;
                        }
                        break;
                    }
                    else
                    {
                        w++;
                    }
                }

                if (done)
                {
                    var c = SpaceTime.CurrentSpaceTime.Add(new Ceiling());
                    c.MoveTo(Left, Top);
                    c.ResizeTo(w, h);
                    break;
                }
            }
        }
    }

    [SpacialElementBinding(typeof(Ceiling))]
    public class CeilingRenderer : SpacialElementRenderer
    {
        private ConsoleString DefaultStyle => new ConsoleString(" ", backgroundColor: ConsoleColor.Gray);
        public CeilingRenderer() { this.ZIndex = int.MaxValue-1; }
        public override void OnRender() => this.IsVisible = (Element as Ceiling).IsVisible;
        protected override void OnPaint(ConsoleBitmap context) => context.FillRect(DefaultStyle[0], 0, 0,Width,Height);

    }

    public class CeilingReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
            if(item.Symbol == 'c' && item.HasSimpleTag("ceiling"))
            {
                hydratedElement = new Ceiling();
                return true;
            }

            hydratedElement = null;
            return false;
        }
    }
}
