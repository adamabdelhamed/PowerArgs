using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public static class SpacialAwareness
    {
        public const string PassThruTag = "passthru";
        public const string WeaponsPassThruTag = "WeaponsPassThru";

        public static Task AnimateAsync(this RectF rectangular, RectangularAnimationOptions options)
        {
            return AnimateAsync(new ColliderBox(rectangular), options);
        }
        public static async Task AnimateAsync(this ICollider rectangular, RectangularAnimationOptions options)
        {
            var startX = rectangular.Left();
            var startY = rectangular.Top();
            var startW = rectangular.Bounds.Width;
            var startH = rectangular.Bounds.Height;

            await Animator.AnimateAsync(new FloatAnimatorOptions()
            {
                Duration = options.Duration,
                AutoReverse = options.AutoReverse,
                AutoReverseDelay = options.AutoReverseDelay,
                DelayProvider = options.DelayProvider,
                Loop = options.Loop,
                EasingFunction = options.EasingFunction,
                From = 0,
                To = 1,
                IsCancelled = options.IsCancelled,
                IsPaused = options.IsPaused,
                OnSet = options.OnSet,
                Setter = v =>
                {
                    var dest = options.Destination();
                    var xDelta = dest.Left - startX;
                    var yDelta = dest.Top - startY;
                    var wDelta = dest.Width - startW;
                    var hDelta = dest.Height - startH;

                    var frameX = startX + (v * xDelta);
                    var frameY = startY + (v * yDelta);
                    var frameW = startW + (v * wDelta);
                    var frameH = startH + (v * hDelta);
                    var frameBounds = new RectF(frameX, frameY, frameW, frameH);
                    options.Setter(rectangular, frameBounds);
                }
            });
        }

        public static IEnumerable<Angle> Enumerate360Angles(Angle initialAngle, int increments = 20)
        {
            var opposite = initialAngle.Opposite();

            for(var i = 1; i <= increments; i++)
            {
                if(i == 1)
                {
                    yield return initialAngle;
                }
                else if(i == 1)
                {
                    yield return opposite;
                }
                else
                {
                    var increment = 180f * i / increments;
                    yield return initialAngle.Add(increment);
                    yield return initialAngle.Add(-increment);
                }
            }
        }
    }

    public class SpacialElementAnimationOptions : RectangularAnimationOptions
    {
        public override void Setter(ICollider target, in RectF bounds)
        {
            (target as SpacialElement).MoveTo(bounds.Left, bounds.Top);
            if (target.Bounds.Width != bounds.Width || target.Bounds.Height != bounds.Height)
            {
                (target as SpacialElement).ResizeTo(bounds.Width, bounds.Height);
            }
        }
    }

    public class ConsoleControlAnimationOptions : RectangularAnimationOptions
    {
        public override void Setter(ICollider target, in RectF bounds)
        {
            (target as ConsoleControl).X = ConsoleMath.Round(bounds.Left);
            (target as ConsoleControl).Y = ConsoleMath.Round(bounds.Top);
            (target as ConsoleControl).Width = ConsoleMath.Round(bounds.Width);
            (target as ConsoleControl).Height = ConsoleMath.Round(bounds.Height);
        }
    }

    public abstract class RectangularAnimationOptions
    {
        public Func<RectF> Destination { get; set; }

        public abstract void Setter(ICollider target, in RectF bounds);

        public EasingFunction EasingFunction { get; set; } = new FloatAnimatorOptions().EasingFunction;
        public double Duration { get; set; } = new FloatAnimatorOptions().Duration;
 
        /// <summary>
        /// If true then the animation will automatically reverse itself when done
        /// </summary>
        public bool AutoReverse { get; set; }

        /// <summary>
        /// When specified, the animation will loop until this lifetime completes
        /// </summary>
        public ILifetimeManager Loop { get; set; }

        /// <summary>
        /// The provider to use for delaying between animation frames
        /// </summary>
        public IDelayProvider DelayProvider { get; set; }

        /// <summary>
        /// If auto reverse is enabled, this is the pause, in milliseconds, after the forward animation
        /// finishes, to wait before reversing
        /// </summary>
        public float AutoReverseDelay { get; set; } = 0;

        /// <summary>
        /// A callback that indicates that the animation should end early
        /// </summary>
        public Func<bool> IsCancelled { get; set; }

        /// <summary>
        /// A callback that indicates that the animation should pause
        /// </summary>
        public Func<bool> IsPaused { get; set; }

        /// <summary>
        /// A callback that is called before a value is set. The parameter is the percentage done.
        /// </summary>
        public Action<float> OnSet { get; set; }
    }
}
