using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public class SpacialElement : TimeFunction, IRectangular
    {
        public Event SizeOrPositionChanged { get; private set; } = new Event();
        public float Left { get; private set; }
        public float Top { get; private set; }
        public int ZIndex { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }

        public List<string> Tags { get; set; } = new List<string>();

        public bool HasTag(string t) => Tags.Contains(t);

        public SpacialElementRenderer Renderer { get; set; }

        public IRectangular Bounds => Rectangular.Create(Left, Top, Width, Height);

        public float CenterX => Left + (Width / 2);
        public float CenterY => Top + (Height / 2);

        internal SpacialElementInternalState InternalSpacialState => InternalState as SpacialElementInternalState;

        public SpacialElement(float w = 1, float h = 1, float x = 0, float y = 0)
        {
            Width = w;
            Height = h;
            Left = x;
            Top = y;
            this.InternalState = new SpacialElementInternalState();
        }

        public override void Initialize() { }
        public override void Evaluate() { }

        public bool IsOneOfThese(List<Type> these)
        {
            Type SpacialElementType = GetType();

            var count = these.Count;
            for (int i = 0; i < count; i++)
            {
                if (these[i] == SpacialElementType || SpacialElementType.IsSubclassOf(these[i])) return true;
            }

            return false;
        }

        public void MoveTo(float x, float y, int? z = null)
        {
            Time.AssertTimeThread();

            x = x < 0 ? 0 : x;
            y = y < 0 ? 0 : y;

            if(x+Width > SpaceTime.CurrentSpaceTime.Width)
            {
                x = SpaceTime.CurrentSpaceTime.Width - Width;
            }

            if (y + Height> SpaceTime.CurrentSpaceTime.Height)
            {
                y = SpaceTime.CurrentSpaceTime.Height - Height;
            }

            this.Left = x;
            this.Top = y;
            if (z.HasValue)
            {
                this.ZIndex = z.Value;
            }

            SizeOrPositionChanged.Fire();
        }

        public void ResizeTo(float w, float h)
        {
#if DEBUG
            Time.AssertTimeThread();
#endif

            this.Width = w;
            this.Height = h;
            SizeOrPositionChanged.Fire();
        }

        public void MoveBy(float dx, float dy, int? dz = null)
        {
            var newX = Left + dx;
            var newY = Top + dy;
            int? newZ = dz.HasValue ? ZIndex + dz.Value : new Nullable<int>();
            MoveTo(newX, newY, newZ);
        }

        public void ResizeBy(float dw, float dh)
        {
            var newW = Width + dw;
            var newH = Height + dh;
            ResizeTo(newW, newH);
        }
    }

    public class SpacialElementInternalState : TimeFunctionInternalState
    {
        internal bool Changed { get; set; }
    }

    public class SpaceTime : Time
    {
        public Random Random { get; set; } = new Random();

        public static SpaceTime CurrentSpaceTime => CurrentTime as SpaceTime;
        public Event<SpacialElement> SpacialElementAdded { get; private set; } = new Event<SpacialElement>();
        public Event<SpacialElement> SpacialElementRemoved { get; private set; } = new Event<SpacialElement>();
        public float Width { get; private set; }
        public float Height { get; private set; }
        public IRectangular Bounds { get; private set; }
        public void ClearChanges() => ChangeTracker.ClearChanges();
        public bool ChangeTrackingEnabled { get => ChangeTracker.Enabled; set => ChangeTracker.Enabled = value; }
        public IEnumerable<SpacialElement> ChangedElements => ChangeTracker.ChangedElements;
        public IEnumerable<SpacialElement> AddedElements => ChangeTracker.AddedElements;
        public IEnumerable<SpacialElement> RemovedElements => ChangeTracker.RemovedElements;
        public IEnumerable<SpacialElement> Elements => Functions.Where(f => f is SpacialElement).Select(f => f as SpacialElement);

        private SpacialChangeTracker ChangeTracker { get; set; } = new SpacialChangeTracker();
        private IDisposable addedSub, removedSub;

        public SpaceTime(float width, float height, TimeSpan? increment = null, TimeSpan? now = null) : base(increment, now)
        {
            this.Width = width;
            this.Height = height;
            this.Bounds = Rectangular.Create(0, 0, Width, Height);
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

        public float CalculateDistanceBetween(SpacialElement a, SpacialElement b)
        {
            return (float)Math.Sqrt(((a.CenterX - b.CenterX) * (a.CenterX - b.CenterX)) + ((a.CenterY - b.CenterY) * (a.CenterY - b.CenterY)));
        }

        public bool IsEmpty(float x, float y, float w, float h)
        {
            var myRect = Rectangular.Create(0, 0, Width, Height);
            foreach (var element in Elements)
            {
                if (myRect.NumberOfPixelsThatOverlap(element) > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsInBounds(IRectangular bounds) => !IsOutOfBounds(bounds);
      

        public bool IsOutOfBounds(IRectangular bounds)
        {
            var testBounds = Rectangular.Create(0, 0, Width, Height);
            var overlap = testBounds.OverlapPercentage(bounds);
            return overlap < 1;
        }

        public bool TryGetEmptyOneUnitLocation(out float x, out float y, int maxAttempts = 10)
        {
            int attempts = 0;
            while (attempts++ < maxAttempts)
            {
                x = Random.Next(0, (int)Width);
                y = Random.Next(0, (int)Height);
                var testBounds = Rectangular.Create(x, y, 1, 1);
                if (Elements.Where(t => t.Contains(testBounds)).Count() == 0)
                {
                    return true;
                }
            }
            x = -1;
            y = -1;
            return false;
        }
    }

    public class SpacialChangeTracker
    {
        private List<SpacialElement> added = new List<SpacialElement>();
        private List<SpacialElement> removed = new List<SpacialElement>();
        private List<SpacialElement> changed = new List<SpacialElement>();
        private Lifetime enablementLifetime = null;

        public bool Enabled
        {
            get
            {
                return enablementLifetime != null;
            }
            set
            {
                if (value && !Enabled)
                {
                    Enable();
                }
                else if (!value && Enabled)
                {
                    Disable();
                }
            }
        }

        public IEnumerable<SpacialElement> ChangedElements => changed.AsReadOnly();
        public IEnumerable<SpacialElement> AddedElements => added.AsReadOnly();
        public IEnumerable<SpacialElement> RemovedElements => removed.AsReadOnly();

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

        private void Enable()
        {
#if DEBUG
            Time.AssertTimeThread();
#endif
            enablementLifetime = new Lifetime();
            enablementLifetime.OnDisposed(() => { enablementLifetime = null; });
            foreach (var element in SpaceTime.CurrentSpaceTime.Elements)
            {
                ConnectToElement(element);
            }

            SpaceTime.CurrentSpaceTime.SpacialElementAdded
                .SubscribeForLifetime((element) => ConnectToElement(element), enablementLifetime);
        }

        private void Disable()
        {
#if DEBUG
            Time.AssertTimeThread();
#endif
            enablementLifetime.Dispose();
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
                if (element.InternalSpacialState.Changed == false)
                {
                    changed.Add(element);
                    element.InternalSpacialState.Changed = true;
                }
            }, Lifetime.EarliestOf(enablementLifetime, element.Lifetime));
        }
    }
}
