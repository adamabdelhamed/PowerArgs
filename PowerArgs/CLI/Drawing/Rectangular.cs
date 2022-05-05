using PowerArgs.Cli.Physics;

namespace PowerArgs.Cli
{

    public class Rectangular : ObservableObject, ICollider
    {
        private int x, y, w, h;
        private RectF fBounds;


        private int z;
 
        public int ZIndex { get => z; set => SetHardIf(ref z, value, z != value); }

        public int ColliderHashCode { get; internal set; }

        public RectF Bounds
        {
            get { return fBounds; }
            set
            {
                fBounds = value;
                var newX = ConsoleMath.Round(value.Left);
                var newY = ConsoleMath.Round(value.Top);
                var newW = ConsoleMath.Round(value.Width);
                var newH = ConsoleMath.Round(value.Height);
                if (newX == x && newY == y && newW == w && newH == h) return;

                x = newX;
                y = newY;
                w = newW;
                h = newH;

                FirePropertyChanged(nameof(Bounds));
            }
        }

        public virtual bool CanCollideWith(ICollider other) => true;

        public int Width
        {
            get
            {
                return w;
            }
            set
            {
                if (w == value) return;
                w = value;
                fBounds = new RectF(fBounds.Left, fBounds.Top, w, fBounds.Height);
                FirePropertyChanged(nameof(Bounds));
            }
        }
        public int Height
        {
            get
            {
                return h;
            }
            set
            {
                if (h == value) return;
                h = value;
                fBounds = new RectF(fBounds.Left, fBounds.Top, fBounds.Width, h);
                FirePropertyChanged(nameof(Bounds));
            }
        }
        public int X
        {
            get
            {
                return x;
            }
            set
            {
                if (x == value) return;
                x = value;
                fBounds = new RectF(x, fBounds.Top, fBounds.Width, fBounds.Height);
                FirePropertyChanged(nameof(Bounds));
            }
        }
        public int Y
        {
            get
            {
                return y;
            }
            set
            {
                if (y == value) return;
                y = value;
                fBounds = new RectF(fBounds.Left, y, fBounds.Width, fBounds.Height);
                FirePropertyChanged(nameof(Bounds));
            }
        }

        public float Left => X;

        public float Top => Y;

        public virtual RectF MassBounds => new RectF(X, Y, Width, Height);
    }
}
