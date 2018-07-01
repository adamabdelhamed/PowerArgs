namespace PowerArgs.Cli
{
    public class ConsoleBitmapViewer : ConsoleControl
    {
        public ConsoleBitmap Bitmap { get; set; }

        public ConsoleBitmapViewer()
        {
            this.CanFocus = false;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            for(var x = 0; x < Width && x < Bitmap.Width; x++)
            {
                for(var y = 0; y < Height & Y < Bitmap.Height; y++)
                {
                    var c = Bitmap.GetPixel(x, y).Value;
                    if(c.HasValue)
                    {
                        context.Pen = c.Value;
                        context.DrawPoint(x, y);
                    }
                }
            }
        }
    }
}
