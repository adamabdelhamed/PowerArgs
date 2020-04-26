using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public interface ISpacialElement : IRectangularF
    {
        Lifetime Lifetime { get; }

        void MoveTo(float x, float y, int? z = null);
    }

    public class SpacialElement : TimeFunction, ISpacialElement
    {
        public Event SizeOrPositionChanged { get; private set; } = new Event();
        public float Left { get; private set; }
        public float Top { get; private set; }
        public int ZIndex { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public CompositionMode CompositionMode { get; set; } = CompositionMode.BlendBackground;
        public RGB BackgroundColor { get; set; } = RGB.Red;

        public ConsoleCharacter? Pen { get; set; }

        internal SpacialElementRenderer Renderer { get; set; }

        public IRectangularF Bounds => RectangularF.Create(Left, Top, Width, Height);

        public float CenterX => Left + (Width / 2);
        public float CenterY => Top + (Height / 2);

        internal SpacialElementInternalState InternalSpacialState => InternalState as SpacialElementInternalState;

        public ObservableObject ObservableProperties { get; private set; } = new ObservableObject();

        public T GetProperty<T>(string key, Func<T> defaultValue = null)
        {
            if(defaultValue != null && ObservableProperties.ContainsKey(key) == false)
            {
                return defaultValue();
            }
            else
            {
                return ObservableProperties.Get<T>(key);
            }
        }

        public void SetProperty<T>(string key, T val) => ObservableProperties.Set(val, key);



        public SpacialElement(float w = 1, float h = 1, float x = 0, float y = 0, int z = 0)
        {
            Width = w;
            Height = h;
            Left = x;
            Top = y;
            ZIndex = z;
            this.InternalState = new SpacialElementInternalState();
            if(GetType() == typeof(SpacialElement))
            {
                Governor.Rate = TimeSpan.FromSeconds(-1);
            }
        }

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

            if(float.IsNaN(x))
            {
                x = 0;
            }

            if (float.IsNaN(y))
            {
                y = 0;
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

    public interface IHaveMassBounds : ISpacialElement
    {
        IRectangularF MassBounds { get; }
        bool IsPartOfMass(SpacialElement other);
        IEnumerable<SpacialElement> Elements { get; }
    }

    public static class IHaveMassBoundsEx
    {
        public static IRectangularF CalculateMassBounds(this IHaveMassBounds mass)
        {
            var left = float.MaxValue;
            var top = float.MaxValue;
            var right = float.MinValue;
            var bottom = float.MinValue;

            foreach (var part in mass.Elements.As<ISpacialElement>().Union(new ISpacialElement[] { mass }))
            {
                left = Math.Min(left, part.Left);
                top = Math.Min(top, part.Top);
                right = Math.Max(right, part.Right());
                bottom = Math.Max(bottom, part.Bottom());
            }

            var bounds = RectangularF.Create(left, top, right - left, bottom - top);
            return bounds;
        }
    }


    public interface IAmMass : ISpacialElement
    {
        IHaveMassBounds Parent { get; }
    }

    public interface IGhost
    {
        bool IsGhost { get; set; }
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
        public IRectangularF Bounds { get; private set; }
        public void ClearChanges() => ChangeTracker.ClearChanges();
        public bool ChangeTrackingEnabled { get => ChangeTracker.Enabled; set => ChangeTracker.Enabled = value; }
        public IReadOnlyList<SpacialElement> ChangedElements => ChangeTracker.ChangedElements;
        public IReadOnlyList<SpacialElement> AddedElements => ChangeTracker.AddedElements;
        public IReadOnlyList<SpacialElement> RemovedElements => ChangeTracker.RemovedElements;
        public IEnumerable<SpacialElement> Elements => Functions.Where(f => f is SpacialElement).Select(f => f as SpacialElement);

        private SpacialChangeTracker ChangeTracker { get; set; } = new SpacialChangeTracker();
        private IDisposable addedSub, removedSub;

        public SpaceTime(float width, float height, TimeSpan? increment = null, TimeSpan? now = null) : base(increment, now)
        {
            this.Width = width;
            this.Height = height;
            this.Bounds = RectangularF.Create(0, 0, Width, Height);
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
            var myRect = RectangularF.Create(0, 0, Width, Height);
            foreach (var element in Elements)
            {
                if (myRect.NumberOfPixelsThatOverlap(element) > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsInBounds(IRectangularF bounds) => !IsOutOfBounds(bounds);
      

        public bool IsOutOfBounds(IRectangularF bounds)
        {
            var testBounds = RectangularF.Create(0, 0, Width, Height);
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
                var testBounds = RectangularF.Create(x, y, 1, 1);
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

        public IReadOnlyList<SpacialElement> ChangedElements => changed.AsReadOnly();
        public IReadOnlyList<SpacialElement> AddedElements => added.AsReadOnly();
        public IReadOnlyList<SpacialElement> RemovedElements => removed.AsReadOnly();

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
                if(Time.CurrentTime == null)
                {
                    throw new InvalidOperationException("Change did not occur on the time thread");
                }

                if (element.InternalSpacialState.Changed == false)
                {
                    changed.Add(element);
                    element.InternalSpacialState.Changed = true;
                }
            }, Lifetime.EarliestOf(enablementLifetime, element.Lifetime));
        }
    }
}
