namespace PowerArgs.Cli
{
    public class GrayscaleFilter : IConsoleControlFilter
    {
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
                            pixel.Value = new ConsoleCharacter(pixel.Value.Value.Value, RGB.Gray);
                        }

                        if (pixel.Value.Value.BackgroundColor != RGB.Black)
                        {
                            pixel.Value = new ConsoleCharacter(pixel.Value.Value.Value, pixel.Value.Value.ForegroundColor, RGB.Gray);
                        }
                    }
                }
            }
        }
    }
}
