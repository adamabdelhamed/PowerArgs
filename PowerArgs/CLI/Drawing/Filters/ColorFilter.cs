namespace PowerArgs.Cli
{
    public class ColorFilter : IConsoleControlFilter
    {
        public RGB Color { get; set; }

        public ColorFilter(RGB color)
        {
            this.Color = color;
        }

        /// <summary>
        /// The control to filter
        /// </summary>
        public ConsoleControl Control { get; set; }

        public void Filter(ConsoleBitmap bitmap)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.Value.HasValue)
                    {
                        if (pixel.Value.Value.BackgroundColor != pixel.Value.Value.ForegroundColor && pixel.Value.Value.BackgroundColor == RGB.Black && pixel.Value.Value != ' ')
                        {
                            pixel.Value = new ConsoleCharacter(pixel.Value.Value.Value, Color);
                        }

                        if (pixel.Value.Value.BackgroundColor != RGB.Black)
                        {
                            pixel.Value = new ConsoleCharacter(pixel.Value.Value.Value, pixel.Value.Value.ForegroundColor, Color);
                        }
                    }
                }
            }
        }
    }
}
