using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class ColorFilter : IConsoleControlFilter
    {
        public RGB Color { get; set; }

        public ColorFilter(in RGB color)
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
                   
                    if (pixel.BackgroundColor != pixel.ForegroundColor && pixel.BackgroundColor == RGB.Black && pixel.Value != ' ')
                    {
                        bitmap.SetPixel(x, y, new ConsoleCharacter(pixel.Value, Color));
                    }

                    if (pixel.BackgroundColor != RGB.Black)
                    {
                        bitmap.SetPixel(x,y, new ConsoleCharacter(pixel.Value, pixel.ForegroundColor, Color));
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

                    bitmap.SetPixel(x, y, new ConsoleCharacter(pixel.Value, pixel.ForegroundColor.ToOther(RGB.Black, Percentage),
                        pixel.BackgroundColor.ToOther(RGB.Black, Percentage)));
                
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
                    bitmap.SetPixel(x, y, new ConsoleCharacter(pixel.Value, RGB.Black.ToOther(pixel.ForegroundColor, Percentage),
                        RGB.Black.ToOther(pixel.BackgroundColor, Percentage)));
                }
            }
        }
    }

    public class AnimationFilter : IConsoleControlFilter
    {
        public float Percentage { get; set; }
        public RGB From { get; set; }
        public RGB To { get; set; }

        /// <summary>
        /// The control to filter
        /// </summary>
        public ConsoleControl Control { get; set; }

        public void Filter(ConsoleBitmap bitmap)
        {
            var targetColor = From.ToOther(To, Percentage);
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);

                    if (pixel.BackgroundColor != pixel.ForegroundColor && pixel.BackgroundColor == RGB.Black && pixel.Value != ' ')
                    {
                        bitmap.SetPixel(x, y, new ConsoleCharacter(pixel.Value, targetColor));
                    }

                    if (pixel.BackgroundColor != RGB.Black)
                    {
                        bitmap.SetPixel(x, y, new ConsoleCharacter(pixel.Value, pixel.ForegroundColor, targetColor));
                    }
                }
            }
        }
    }


    public static class FadeEx
    {
        public static async Task<AnimationFilter> FadeAnimate(this ConsoleControl c, RGB from, RGB to, float duration = 500, EasingFunction easingFunction = null, float fromPerecntage = 0, float toPercentage = 1)
        {
            easingFunction = easingFunction ?? Animator.Linear;
            var filter = new AnimationFilter() { From = from, To = to  };
            c.RenderFilters.Add(filter);

            await Animator.AnimateAsync(new FloatAnimatorOptions()
            {
                From = fromPerecntage,
                To = toPercentage,
                Duration = duration,
                EasingFunction = (p) => easingFunction(p),
                Setter = p =>
                {
                    filter.Percentage = p;
                    ConsoleApp.Current.RequestPaint();
                }
            });
            return filter;
        }

        public static async Task<FadeInFilter> FadeIn(this ConsoleControl c, float duration = 500, EasingFunction easingFunction = null, float percentage = 1)
        {
            easingFunction = easingFunction ?? Animator.Linear;
            var filter = new FadeInFilter();
            c.RenderFilters.Add(filter);

            await Animator.AnimateAsync(new FloatAnimatorOptions()
            {
                From = 0, 
                To = percentage,
                Duration = duration,
                EasingFunction = (p)=> easingFunction(p),
                Setter = p =>
                {
                    filter.Percentage = p;
                    ConsoleApp.Current.RequestPaint();
                }
            });
            return filter;
        }

        public static async Task<FadeOutFilter> FadeOut(this ConsoleControl c, float duration = 500, EasingFunction easingFunction = null, float percentage = 1)
        {
            easingFunction = easingFunction ?? Animator.Linear;
            var filter = new FadeOutFilter();
            c.RenderFilters.Add(filter);

            await Animator.AnimateAsync(new FloatAnimatorOptions()
            {
                From = 0,
                To = percentage,
                Duration = duration,
                EasingFunction = (p) => easingFunction(p),
                Setter = p =>
                {
                    filter.Percentage = p;
                    ConsoleApp.Current.RequestPaint();
                }
            });
            return filter;
        }
    }
}
