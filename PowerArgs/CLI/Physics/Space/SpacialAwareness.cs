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


        public static float LineOfSightVisibility(this IRectangularF from, float angle, IEnumerable<IRectangularF> obstacles, float range, float increment = .5f)
        {
            for (var d = increment; d < range; d += increment)
            {
                var testLocation = from.Center().MoveTowards(angle, d);
                var testRect = RectangularF.Create(testLocation.Left - from.Width/2, testLocation.Top - from.Height/2, from.Width, from.Height);
                if (obstacles.Where(o => o.Touches(testRect)).Any() || SpaceTime.CurrentSpaceTime.Bounds.Contains(testRect) == false)
                {
                    return d;
                }
            }

            return range;
        }



        public static ILocationF MoveTowards(this ILocationF a, ILocationF b, float distance)
        {
            float slope = (a.Top - b.Top) / (a.Left - b.Left);
            bool forward = a.Left <= b.Left;
            bool up = a.Top <= b.Top;

            float abDistance = Geometry.CalculateNormalizedDistanceTo(a,b);
            double angle = Math.Asin(Math.Abs(b.Top - a.Top) / abDistance);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

            float x2 = forward ? a.Left + dx : a.Left - dx;
            float y2 = up ? a.Top + dy : a.Top - dy;

            var ret = LocationF.Create(x2, y2);
            return ret;
        }

        public static IRectangularF MoveTowards(this IRectangularF r, float angle, float distance, bool normalized = true)
        {
            var newLoc = MoveTowards(r.TopLeft(), angle, distance, normalized);
            var ret = RectangularF.Create(newLoc.Left, newLoc.Top, r.Width, r.Height);
            return ret;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static IRectangularF GetOffsetByPixels(this IRectangularF r, float dx, float dy)
        {
            return RectangularF.Create(r.Left + dx, r.Top + dy, r.Width, r.Height);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ILocationF GetOffsetByPixels(this ILocationF r, float dx, float dy)
        {
            return LocationF.Create(r.Left + dx, r.Top + dy);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ILocationF MoveTowards(this ILocationF a, float angle, float distance, bool normalized = true)
        {
            while(angle < 0)
            {
                angle += 360;
            }

            while(angle > 360)
            {
                angle -= 360;
            }
        
            if (normalized)
            {
                distance = Geometry.NormalizeQuantity(distance, angle);
            }
            var forward = angle > 270 || angle < 90;
            var up = angle > 180;

            // convert to radians
            angle = (float)(angle * Math.PI / 180);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

            float x2 = forward ? a.Left + dx : a.Left - dx;
            float y2 = up ? a.Top - dy : a.Top + dy;

            var ret = LocationF.Create(x2, y2);
            return ret;
        }





        public static async Task AnimateAsync(this IRectangularF rectangular, RectangularAnimationOptions options)
        {
            var startX = rectangular.Left;
            var startY = rectangular.Top;
            var startW = rectangular.Width;
            var startH = rectangular.Height;

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
                    var frameBounds = RectangularF.Create(frameX, frameY, frameW, frameH);
                    options.Setter(rectangular, frameBounds);
                }
            });
        }

        public static IRectangularF EffectiveBounds(this IRectangularF rect) => rect is IHaveMassBounds ? (rect as IHaveMassBounds).MassBounds : rect;


        public static IEnumerable<float> Enumerate360Angles(float initialAngle, int increments = 20)
        {
            initialAngle = initialAngle % 360;
            var opposite = initialAngle.GetOppositeAngle();

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
                    yield return initialAngle.AddToAngle(increment);
                    yield return initialAngle.AddToAngle(-increment);
                }
            }
        }

     
      
    }

    public class SpacialElementAnimationOptions : RectangularAnimationOptions
    {
        public override void Setter(IRectangularF target, IRectangularF bounds)
        {
            (target as SpacialElement).MoveTo(bounds.Left, bounds.Top);
            if (target.Width != bounds.Width || target.Height != bounds.Height)
            {
                (target as SpacialElement).ResizeTo(bounds.Width, bounds.Height);
            }
        }
    }

    public class ConsoleControlAnimationOptions : RectangularAnimationOptions
    {
        public override void Setter(IRectangularF target, IRectangularF bounds)
        {
            (target as ConsoleControl).X = Geometry.Round(bounds.Left);
            (target as ConsoleControl).Y = Geometry.Round(bounds.Top);
            (target as ConsoleControl).Width = Geometry.Round(bounds.Width);
            (target as ConsoleControl).Height = Geometry.Round(bounds.Height);
        }
    }

    public abstract class RectangularAnimationOptions
    {
        public Func<IRectangularF> Destination { get; set; }

        public abstract void Setter(IRectangularF target, IRectangularF bounds);

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
