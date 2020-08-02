using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public class SpaceTime : Time
    {
        public Random Random { get; set; } = new Random();

        public static SpaceTime CurrentSpaceTime => CurrentTime as SpaceTime;
        public Event<SpacialElement> SpacialElementAdded { get; private set; } = new Event<SpacialElement>();
        public Event<SpacialElement> SpacialElementRemoved { get; private set; } = new Event<SpacialElement>();
        public float Width { get; private set; }
        public float Height { get; private set; }
        public IRectangularF Bounds { get; private set; }
        public void ClearChanges() => ChangeTracker.ClearChanges();
 
        public IReadOnlyList<SpacialElement> ChangedElements => ChangeTracker.ChangedElements;
        public IReadOnlyList<SpacialElement> AddedElements => ChangeTracker.AddedElements;
        public IReadOnlyList<SpacialElement> RemovedElements => ChangeTracker.RemovedElements;
        public IEnumerable<SpacialElement> Elements => Functions.Where(f => f is SpacialElement).Select(f => f as SpacialElement);

        private SpacialChangeTracker ChangeTracker { get; set; } 
        private IDisposable addedSub, removedSub;

        public SpaceTime(float width, float height, TimeSpan? increment = null, TimeSpan? now = null) : base(increment, now)
        {
            this.Width = width;
            this.Height = height;
            this.Bounds = RectangularF.Create(0, 0, Width, Height);
            Invoke(() =>
            {
                this.ChangeTracker = new SpacialChangeTracker();
                this.OnDisposed(ChangeTracker.Dispose);
            });
            addedSub = this.TimeFunctionAdded.SubscribeUnmanaged((f) =>
            {
                if (f is SpacialElement)
                {
                    SpacialElementAdded.Fire(f as SpacialElement);
                }
            });

            removedSub = this.TimeFunctionRemoved.SubscribeUnmanaged((f) =>
            {
                if (f is SpacialElement)
                {
                    SpacialElementRemoved.Fire(f as SpacialElement);
                }
            });  
        }
    }

    public class SpacialChangeTracker : Lifetime
    {
        private List<SpacialElement> added = new List<SpacialElement>();
        private List<SpacialElement> removed = new List<SpacialElement>();
        private List<SpacialElement> changed = new List<SpacialElement>();
  

        public IReadOnlyList<SpacialElement> ChangedElements => changed.AsReadOnly();
        public IReadOnlyList<SpacialElement> AddedElements => added.AsReadOnly();
        public IReadOnlyList<SpacialElement> RemovedElements => removed.AsReadOnly();

        public SpacialChangeTracker()
        {
#if DEBUG
            Time.AssertTimeThread();
#endif
           
            foreach (var element in SpaceTime.CurrentSpaceTime.Elements)
            {
                ConnectToElement(element);
            }

            SpaceTime.CurrentSpaceTime.SpacialElementAdded
                .SubscribeForLifetime((element) => ConnectToElement(element), this);
            this.OnDisposed(ClearChanges);
        }

        public void ClearChanges()
        {
            foreach (var element in changed)
            {
                element.InternalSpacialState.Changed = false;
            }

            added.Clear();
            removed.Clear();
            changed.Clear();
        }

    
 

        private void ConnectToElement(SpacialElement element)
        {
            added.Add(element);

            element.Lifetime.OnDisposed(() =>
            {
                removed.Add(element);
            });

            element.SizeOrPositionChanged.SubscribeForLifetime(() =>
            {
                if(Time.CurrentTime == null)
                {
                    throw new InvalidOperationException("Change did not occur on the time thread");
                }

                if (element.InternalSpacialState.Changed == false)
                {
                    changed.Add(element);
                    element.InternalSpacialState.Changed = true;
                }
            }, Lifetime.EarliestOf(this, element.Lifetime));
        }
    }
}
