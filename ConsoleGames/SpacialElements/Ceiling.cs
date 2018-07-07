using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
namespace ConsoleGames
{
    public class Ceiling : SpacialElement, IObservableObject
    {
        protected ObservableObject observable;
        public bool SuppressEqualChanges { get; set; }
        public IDisposable SubscribeUnmanaged(string propertyName, Action handler) => observable.SubscribeUnmanaged(propertyName, handler);
        public void SubscribeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        public IDisposable SynchronizeUnmanaged(string propertyName, Action handler) => observable.SynchronizeUnmanaged(propertyName, handler);
        public void SynchronizeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SynchronizeForLifetime(propertyName, handler, lifetimeManager);

        public bool IsVisible { get => observable.Get<bool>(); set => observable.Set(value); }
        public bool Seed { get; set; }
        public Ceiling()
        {
            observable = new ObservableObject(this);
            IsVisible = true;
            this.SubscribeForLifetime(nameof(IsVisible), this.SizeOrPositionChanged.Fire, this.Lifetime);
            Tags.Add("passthru");
        }

        public override void Initialize()
        {
            if (Seed)
            {
                var walls = SpaceTime.CurrentSpaceTime.Elements.WhereAs<Wall>().ToList();
                var done = false;
                for (var y = Top; y < SpaceTime.CurrentSpaceTime.Height; y++)
                {
                    for (var x = Left; x < SpaceTime.CurrentSpaceTime.Width; x++)
                    {
                        if (x == Left && y == Top) continue;

                        var canPlaceCeiling = walls.Where(w => w.Touches(PowerArgs.Cli.Physics.Rectangular.Create(x, y, 1, 1))).Count() == 0;

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
        }
    }

    [SpacialElementBinding(typeof(Ceiling))]
    public class CeilingRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Gray);
        public CeilingRenderer() { this.ZIndex = int.MaxValue-1; }
        public override void OnRender() => this.IsVisible = (Element as Ceiling).IsVisible;
        
    }

    public class CeilingReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
            if(item.Symbol == 'c' && item.HasSimpleTag("ceiling"))
            {
                hydratedElement = new Ceiling() { Seed = true };
                return true;
            }

            hydratedElement = null;
            return false;
        }
    }
}
