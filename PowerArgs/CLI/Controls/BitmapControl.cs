using System;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A control that displays a ConsoleBitmap
    /// </summary>
    public class BitmapControl : ConsoleControl
    {
        /// <summary>
        /// The Bitmap image to render in the control
        /// </summary>
        public ConsoleBitmap Bitmap { get { return Get<ConsoleBitmap>(); } set { Set(value); } }

        /// <summary>
        /// If true then this control will auto size itself based on its target bitmap
        /// </summary>
        public bool AutoSize { get { return Get<bool>(); } set { Set(value); } }

        /// <summary>
        /// Creates a new Bitmap control
        /// </summary>
        public BitmapControl()
        {
            this.SubscribeForLifetime(nameof(AutoSize), BitmapOrAutoSizeChanged, this.LifetimeManager);
            this.SubscribeForLifetime(nameof(Bitmap), BitmapOrAutoSizeChanged, this.LifetimeManager);
        }

        private void BitmapOrAutoSizeChanged()
        {
            if (AutoSize && Bitmap != null)
            {
                this.Width = Bitmap.Width;
                this.Height = Bitmap.Height;
                Application?.Paint();
            }
        }

        /// <summary>
        /// Draws the bitmap
        /// </summary>
        /// <param name="context">the pain context</param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            if (Bitmap == null) return;
            for (var x = 0; x < Bitmap.Width && x < this.Width; x++)
            {
                for (var y = 0; y < Bitmap.Height && y < this.Height; y++)
                {
                    var pixel = Bitmap.GetPixel(x, y).Value;
                    if (pixel.HasValue)
                    {
                        context.Pen = pixel.Value;
                        context.DrawPoint(x, y);
                    }
                }
            }
        }
    }
}
