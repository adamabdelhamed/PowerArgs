using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace ConsoleGames
{
    public class Ceiling : SpacialElement, IObservableObject
    {
        protected ObservableObject observable;
        public bool SuppressEqualChanges { get; set; }
        public IDisposable SubscribeUnmanaged(string propertyName, Action handler) => observable.SubscribeUnmanaged(propertyName, handler);
        public void SubscribeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager) => observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        public IDisposable SynchronizeUnmanaged(string propertyName, Action handler) => observable.SynchronizeUnmanaged(propertyName, handler);
        public void SynchronizeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager) => observable.SynchronizeForLifetime(propertyName, handler, lifetimeManager);

        public bool IsVisible { get => observable.Get<bool>(); set => observable.Set(value); }

        public Ceiling()
        {
            observable = new ObservableObject(this);
            IsVisible = true;
            this.SubscribeForLifetime(nameof(IsVisible), this.SizeOrPositionChanged.Fire, this.Lifetime.LifetimeManager);
        }
    }

    [SpacialElementBinding(typeof(Ceiling))]
    public class CielingRenderer : SpacialElementRenderer
    {
        public CielingRenderer()
        {
            this.ZIndex = int.MaxValue-1;
            Background = ConsoleColor.Gray;
        }

        public override void OnRender()
        {
            this.IsVisible = (Element as Ceiling).IsVisible;
        }
    }

    public class CeilingReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out SpacialElement hydratedElement)
        {
            if(item.Symbol == 'c')
            {
                hydratedElement = new Ceiling();
                return true;
            }

            hydratedElement = null;
            return false;
        }
    }
}
