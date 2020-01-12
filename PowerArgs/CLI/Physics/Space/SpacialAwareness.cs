using PowerArgs.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public static class SpacialAwareness
    {
        public const string PassThruTag = "passthru";

        public static bool HasLineOfSight(this Character from, IRectangularF to)
        {
            var obstacles = from.GetObstacles(from.Velocity.HitDetectionExclusions, from.Velocity.HitDetectionExclusionTypes);
            return HasLineOfSight(from, to, obstacles);
        }

        public static bool HasLineOfSight(this IRectangularF from, IRectangularF to, IEnumerable<IRectangularF> obstacles, float increment = .25f) => GetLineOfSight(from, to, obstacles, increment) != null;

        private static List<IRectangularF> GetLineOfSight(this IRectangularF from, IRectangularF to, IEnumerable<IRectangularF> obstacles, float increment = .25f)
        {
            IRectangularF current = from;
            var currentDistance = current.CalculateDistanceTo(to);
            var a = current.Center().CalculateAngleTo(to.Center());

            var effectiveObstacles = new List<IRectangularF>(obstacles);// because we will be destructive

            for(var i = 0; i < effectiveObstacles.Count; i++)
            {
                var o = effectiveObstacles[i];
                if(o == from || o == to)
                {
                    effectiveObstacles.RemoveAt(i--);
                    continue;
                }

                var aODiff = from.CalculateAngleTo(o).DiffAngle(a);
                if(aODiff >= 180)
                {
                    effectiveObstacles.RemoveAt(i--);
                    continue;
                }
            }

           
            var path = new List<IRectangularF>();

            var terminate = false;
            while (terminate == false)
            {
                if (currentDistance < increment)
                {
                    currentDistance = 0;
                    current = RectangularF.Create(to.CenterX() - from.Width / 2, to.CenterY() - from.Height / 2, from.Width, from.Height);
                    terminate = true;
                }
                else
                {
                    current = RectangularF.Create(MoveTowards(current.Center(), a, increment), current);
                    current = RectangularF.Create(current.Left - current.Width / 2, current.Top - current.Height / 2, current.Width, current.Height);
                }

                for(var i = 0; i < effectiveObstacles.Count;i++)
                {
                    var obstacle = effectiveObstacles[i];
                    if (obstacle.OverlapPercentage(current) > 0)
                    {
                        return null;
                    }
                }
                path.Add(current);
                currentDistance = current.CalculateDistanceTo(to);
            }

            return path;
        }

        public static bool HasLineOfSightBeta(this IRectangularF from, IRectangularF to, IEnumerable<IRectangularF> obstacles)
        {
            var fromC = from.CenterRect();
            var toC = to.CenterRect();

            var a = from.CalculateAngleTo(toC);
            var d = Geometry.CalculateNormalizedDistanceTo(fromC, toC);

            foreach(var o in obstacles)
            {
                if (o == from || o == to) continue;

                var oc = o.CenterRect();
                var dO = Geometry.CalculateNormalizedDistanceTo(fromC, oc);
                if (dO > d) continue;

                // todo - define this curve smoothly with real math
                var angleDiffThreshold = d < 2 ? 180 :
                                   d < 5 ? 60 :
                                   d < 10 ? 40 :
                                   d < 20 ? 25 :
                                   d < 40 ? 20 : 15;

                var aO = fromC.CalculateAngleTo(oc);
                var aODiff = a.DiffAngle(aO);
                if(aODiff < angleDiffThreshold)
                {
                    return false;
                }
            }

            return true;
        }

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

        public static IRectangularF MoveTowards(this IRectangularF r, float angle, float distance)
        {
            var newLoc = MoveTowards(r as ILocationF, angle, distance);
            var ret = RectangularF.Create(newLoc.Left, newLoc.Top, r.Width, r.Height);
            return ret;
        }

        public static ILocationF MoveTowards(this ILocationF a, float angle, float distance)
        {
            while(angle < 0)
            {
                angle += 360;
            }

            while(angle > 360)
            {
                angle -= 360;
            }

            distance = Geometry.NormalizeQuantity(distance, angle);
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

        public static List<IRectangularF> GetObstacles(this SpacialElement element, IEnumerable<SpacialElement> exclusions = null, IEnumerable<Type> excludedTypes=null)
        {
            var ret = new List<IRectangularF>();
            foreach (var e in SpaceTime.CurrentSpaceTime.Elements)
            {
                if(e == element)
                {
                    continue;
                }
                else if(exclusions != null && exclusions.Contains(e))
                {
                    continue;
                }
                else if(e.ZIndex != element.ZIndex)
                {
                    continue;
                }
                else if(e.HasSimpleTag(PassThruTag))
                {
                    continue;
                }
                else if(excludedTypes != null && excludedTypes.Where(t => e.GetType() == t || e.GetType().IsSubclassOf(t) || e.GetType().GetInterfaces().Contains(t)).Any())
                {
                    continue;
                }
                else
                {
                    ret.Add(e);
                }
            }

            ret.Add(RectangularF.Create(0,-1,SpaceTime.CurrentSpaceTime.Width,1)); // top boundary
            ret.Add(RectangularF.Create(0, SpaceTime.CurrentSpaceTime.Height, SpaceTime.CurrentSpaceTime.Width, 1)); // bottom boundary
            ret.Add(RectangularF.Create(-1,0,1,SpaceTime.CurrentSpaceTime.Height)); // left boundary
            ret.Add(RectangularF.Create(SpaceTime.CurrentSpaceTime.Width, 0, 1, SpaceTime.CurrentSpaceTime.Height)); // right boundary

            return ret;
        }



        public static async Task AnimateAsync(this IRectangularF rectangular, RectangularAnimationOptions options)
        {
            var startX = rectangular.Left;
            var startY = rectangular.Top;
            var startW = rectangular.Width;
            var startH = rectangular.Height;

            var xDelta = options.Destination.Left - startX;
            var yDelta = options.Destination.Top - startY;
            var wDelta = options.Destination.Width - startW;
            var hDelta = options.Destination.Height - startH;

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
                Setter = v =>
                {
                    var frameX = startX + (v * xDelta);
                    var frameY = startY + (v * yDelta);
                    var frameW = startW + (v * wDelta);
                    var frameH = startH + (v * hDelta);
                    var frameBounds = RectangularF.Create(frameX, frameY, frameW, frameH);
                    options.Setter(rectangular, frameBounds);
                }
            });
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
            (target as ConsoleControl).X = (int)Math.Round(bounds.Left);
            (target as ConsoleControl).Y = (int)Math.Round(bounds.Top);
            (target as ConsoleControl).Width = (int)Math.Round(bounds.Width);
            (target as ConsoleControl).Height = (int)Math.Round(bounds.Height);
        }
    }

    public abstract class RectangularAnimationOptions
    {
        public IRectangularF Destination { get; set; }

        public abstract void Setter(IRectangularF target, IRectangularF bounds);

        public Func<float, float> EasingFunction { get; set; } = new FloatAnimatorOptions().EasingFunction;
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
    }
}
