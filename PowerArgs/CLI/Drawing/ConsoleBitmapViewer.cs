namespace PowerArgs.Cli
{
    /// <summary>
    /// A control that can render a ConsoleBitmap
    /// </summary>
    public class ConsoleBitmapViewer : ConsoleControl
    {
        /// <summary>
        /// The bitmap to render
        /// </summary>
        public ConsoleBitmap Bitmap { get => Get<ConsoleBitmap>(); set => Set(value); }

        /// <summary>
        /// Creates a new console bitmap viewer
        /// </summary>
        public ConsoleBitmapViewer()
        {
            this.CanFocus = false;
        }

        /// <summary>
        /// Pains the target bitmap
        /// </summary>
        /// <param name="context"></param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            if (Bitmap == null) return;

            for(var x = 0; x < Width && x < Bitmap.Width; x++)
            {
                for(var y = 0; y < Height && y < Bitmap.Height; y++)
                {
                    var c = Bitmap.GetPixel(x, y).Value;
                    context.Pen = c;
                    context.DrawPoint(x, y);
                }
            }
        }
    }
}
