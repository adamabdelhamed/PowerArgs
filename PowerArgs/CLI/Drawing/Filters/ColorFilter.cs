using System.Threading.Tasks;

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
                   
                    if (pixel.Value.BackgroundColor != pixel.Value.ForegroundColor && pixel.Value.BackgroundColor == RGB.Black && pixel.Value.Value != ' ')
                    {
                        pixel.Value = new ConsoleCharacter(pixel.Value.Value, Color);
                    }

                    if (pixel.Value.BackgroundColor != RGB.Black)
                    {
                        pixel.Value = new ConsoleCharacter(pixel.Value.Value, pixel.Value.ForegroundColor, Color);
                    }
                }
            }
        }
    }

    public class FadeOutFilter : IConsoleControlFilter
    {
        public float Percentage { get; set; }

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

                    if (pixel.Value.BackgroundColor != pixel.Value.ForegroundColor && pixel.Value.BackgroundColor == RGB.Black && pixel.Value.Value != ' ')
                    {
                        pixel.Value = new ConsoleCharacter(pixel.Value.Value, pixel.Value.ForegroundColor.ToOther(RGB.Black, Percentage));
                    }

                    if (pixel.Value.BackgroundColor != RGB.Black)
                    {
                        pixel.Value = new ConsoleCharacter(pixel.Value.Value, pixel.Value.ForegroundColor, pixel.Value.BackgroundColor.ToOther(RGB.Black, Percentage));
                    }
                }
            }
        }
    }

    public class FadeInFilter : IConsoleControlFilter
    {
        public float Percentage { get; set; }

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

                    if (pixel.Value.BackgroundColor != pixel.Value.ForegroundColor && pixel.Value.BackgroundColor == RGB.Black && pixel.Value.Value != ' ')
                    {
                        pixel.Value = new ConsoleCharacter(pixel.Value.Value, RGB.Black.ToOther(pixel.Value.ForegroundColor, Percentage));
                    }

                    if (pixel.Value.BackgroundColor != RGB.Black)
                    {
                        pixel.Value = new ConsoleCharacter(pixel.Value.Value, pixel.Value.ForegroundColor, RGB.Black.ToOther(pixel.Value.BackgroundColor, Percentage));
                    }
                }
            }
        }
    }

    public static class FadeEx
    {
        public static async Task<IConsoleControlFilter> FadeIn(this ConsoleControl c, float duration = 500, EasingFunction easingFunction = null)
        {
            easingFunction = easingFunction ?? Animator.Linear;
            var filter = new FadeInFilter();
            c.RenderFilters.Add(filter);

            await Animator.AnimateAsync(new FloatAnimatorOptions()
            {
                From = 0, 
                To = 1,
                Duration = duration,
                EasingFunction = (p)=> easingFunction(p),
                Setter = p =>
                {
                    filter.Percentage = p;
                    ConsoleApp.Current.Paint();
                }
            });
            return filter;
        }

        public static async Task<IConsoleControlFilter> FadeOut(this ConsoleControl c, float duration = 500, EasingFunction easingFunction = null)
        {
            easingFunction = easingFunction ?? Animator.Linear;
            var filter = new FadeOutFilter();
            c.RenderFilters.Add(filter);

            await Animator.AnimateAsync(new FloatAnimatorOptions()
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = (p) => easingFunction(p),
                Setter = p =>
                {
                    filter.Percentage = p;
                    ConsoleApp.Current.Paint();
                }
            });
            return filter;
        }
    }
}
