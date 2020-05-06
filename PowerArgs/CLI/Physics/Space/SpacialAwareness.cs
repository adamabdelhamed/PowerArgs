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

        public static IRectangularF MoveTowards(this IRectangularF r, float angle, float distance)
        {
            var newLoc = MoveTowards(r.TopLeft(), angle, distance);
            var ret = RectangularF.Create(newLoc.Left, newLoc.Top, r.Width, r.Height);
            return ret;
        }


        public static IRectangularF GetOffsetByPixels(this IRectangularF r, float dx, float dy)
        {
            return RectangularF.Create(r.Left + dx, r.Top + dy, r.Width, r.Height);
        }

        public static ILocationF GetOffsetByPixels(this ILocationF r, float dx, float dy)
        {
            return LocationF.Create(r.Left + dx, r.Top + dy);
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

        public static List<IRectangularF> GetObstacles(this SpacialElement element, float? z = null)
        {
            float effectiveZ = z.HasValue ? z.Value : element.ZIndex;
            var v = Velocity.For(element);
            IEnumerable<SpacialElement> exclusions = v?.HitDetectionExclusions;
            IEnumerable<Type> excludedTypes = v?.HitDetectionExclusionTypes;
            Func<IEnumerable<SpacialElement>> dynamicExclusions =  v?.HitDetectionDynamicExclusions;

            var ret = new List<IRectangularF>();
            var dynamicEx = dynamicExclusions != null ? dynamicExclusions.Invoke() : null;
            foreach (var e in SpaceTime.CurrentSpaceTime.Elements)
            {
                if(e == element)
                {
                    continue;
                }
                else if (e.ZIndex != effectiveZ)
                {
                    continue;
                }
                else if(exclusions != null && exclusions.Contains(e))
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
                else if(dynamicEx != null && dynamicEx.Contains(e))
                {
                    continue;
                }
                else if(element is IHaveMassBounds && (element as IHaveMassBounds).IsPartOfMass(e))
                {
                    // own mass can't obstruct itself
                    continue;
                }
                else if (e is WeaponElement && (e as WeaponElement).Weapon?.Holder == element)
                {
                    // Characters can't hit their own weapon elements
                    continue;
                }
                else if (e is WeaponElement && (e as WeaponElement).Weapon?.Holder != null && element is IHaveMassBounds && (element as IHaveMassBounds).IsPartOfMass((e as WeaponElement).Weapon?.Holder))
                {
                    // Characters can't hit their own weapon elements
                    continue;
                }
                else if (element is WeaponElement && e.HasSimpleTag(WeaponsPassThruTag))
                {
                    continue;
                }
                else if (element is Character && e.HasSimpleTag(WeaponsPassThruTag))
                {
                    continue;
                }
                else if (element is WeaponElement && (element as WeaponElement).Weapon?.Holder == e)
                {
                    // Characters can't hit their own weapon elements
                    continue;
                }
                else if (e is WeaponElement && element is WeaponElement &&
                  (e as WeaponElement).Weapon?.Holder == (element as WeaponElement).Weapon?.Holder)
                {
                    if (e is Explosive || element is Explosive)
                    {
                        if (e is WeaponElement && (e as WeaponElement).Weapon?.Style == WeaponStyle.Shield)
                        {
                            continue;
                        }
                        else if (e is WeaponElement && (e as WeaponElement).Weapon?.Style == WeaponStyle.Shield)
                        {
                            continue;
                        }
                        else
                        {
                            ret.Add(e);
                        }
                    }
                    else
                    {
                        // WeaponElements from the same holder don't collide with each other
                        continue;
                    }
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

        public class NudgeEvent
        {
            public SpacialElement Element { get; set; }
            public bool Success { get; set; }
        }

        public static Event<NudgeEvent> OnNudge { get; private set; } = new Event<NudgeEvent>();
        public static NudgeEvent NudgeFree(this SpacialElement el, IRectangularF desiredLocation = null, float optimalAngle = 0, int? z = null)
        {
            var loc = GetNudgeLocation(el, desiredLocation, optimalAngle, z);
            if (loc != null)
            {
                if (el is IHaveMassBounds == false)
                {
                    el.MoveTo(loc.Left, loc.Top, z);
                }
                else
                {
                    var elBounds = el.EffectiveBounds();
                    var dx =  el.Left - elBounds.Left;
                    var dy = el.Top - elBounds.Top;
                    el.MoveTo(loc.Left + dx, loc.Top + dy, z);
                }
                var ev = new NudgeEvent() { Element = el, Success = true };
                OnNudge.Fire(ev);
                return ev;
            }
            else
            {
                var ev = new NudgeEvent() { Element = el, Success = false };
                OnNudge.Fire(ev);
                return ev;
            }
        }

        public static ILocationF GetNudgeLocation(this SpacialElement el, IRectangularF desiredLocation = null, float optimalAngle = 0, int? z = null)
        {
            desiredLocation = desiredLocation ?? el;
            var obstacles = el.GetObstacles(z: z);
            if (obstacles.Where(o => o.Touches(desiredLocation)).Any())
            {
                foreach (var angle in Enumerate360Angles(optimalAngle))
                {
                    for (var d = .1f; d < 15f; d+=.1f)
                    {
                        var effectiveAngle = angle % 360;
                        var testLoc = desiredLocation.MoveTowards(effectiveAngle, d);
                        var testArea = RectangularF.Create(testLoc.Left, testLoc.Top, desiredLocation.Width, desiredLocation.Height);
                        if (obstacles.Where(o => o.Touches(testArea)).None() && SpaceTime.CurrentSpaceTime.Bounds.Contains(testArea))
                        {
                            return testLoc.TopLeft();
                        }
                    }
                }
            }
            return null;
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
