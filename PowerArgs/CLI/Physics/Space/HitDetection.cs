using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public enum HitType
    {
        None = 0,
        Element = 1,
        Boundary = 2,
    }

    public class Impact
    {
        public float Angle { get; set; }
        public IRectangular Bounds { get; set; }
        public SpacialElement ElementHit { get; set; }
    }

    public class HitPrediction
    {
        public HitType Type { get; set; }
        public Direction Direction { get; set; }
        public IRectangular BoundsOfItemBeingHit { get; set; }
        public SpacialElement ElementHit { get; set; }
    }

    public static class HitDetection
    {

        public static HitPrediction PredictHit(SpaceTime r, SpacialElement Target, List<Type> hitDetectionTypes, List<SpacialElement> hitDetectionExclusions, float dx, float dy)
        {
            if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1)
            {
                return PredictHitInternal(r, Target, hitDetectionTypes, hitDetectionExclusions, dx, dy);
            }

            HitPrediction latestResult = null;
            for (var i = 1; i <= 10; i++)
            {
                var dxP = Approach(0, dx, dx / 10 * i);
                var dyP = Approach(0, dy, dy / 10 * i);
                latestResult = PredictHitInternal(r, Target, hitDetectionTypes, hitDetectionExclusions, dxP, dyP);
                if(latestResult.Type != HitType.None)
                {
                    return latestResult;
                }
            }

            return latestResult;
        }

        private static float Approach(float value, float target, float by)
        {
            var gtBefore = value > target;
            var ret = value + by;
            var gtAfter = value > target;
            if (gtAfter != gtBefore)
            {
                ret = target;
            }
            return ret;
        }

        private static HitPrediction PredictHitInternal(SpaceTime r, SpacialElement Target, List<Type> hitDetectionTypes, List<SpacialElement> hitDetectionExclusions, float dx, float dy)
        {
            HitPrediction prediction = new HitPrediction();

            if(dx == 0 && dy == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
                return prediction;
            }

            if (dy > 0 && Target.Bottom() + dy >= r.Height)
            {
                prediction.Direction = Direction.Down;
                prediction.Type = HitType.Boundary;
                prediction.BoundsOfItemBeingHit = Rectangular.Create(Target.Left + dx, r.Bounds.Height + dy, 1, 1);
                return prediction;
            }
            else if (dx < 0 && Target.Left + dx <= 0)
            {
                prediction.Direction = Direction.Left;
                prediction.Type = HitType.Boundary;
                prediction.BoundsOfItemBeingHit = Rectangular.Create(-dx, Target.Top + dy, 1, 1);
                return prediction;
            }
            else if (dy < 0 && Target.Top + dy <= 0)
            {
                prediction.Direction = Direction.Up;
                prediction.Type = HitType.Boundary;
                prediction.BoundsOfItemBeingHit = Rectangular.Create(Target.Left + dx, -dy, 1, 1);
                return prediction;
            }
            else if (dx > 0 && Target.Right() + dx >= r.Width)
            {
                prediction.Direction = Direction.Right;
                prediction.Type = HitType.Boundary;
                prediction.BoundsOfItemBeingHit = Rectangular.Create(r.Width + dx, Target.Top + dy, 1, 1);
                return prediction;
            }

            var testArea = Rectangular.Create(Target.Left + dx, Target.Top + dy, Target.Width, Target.Height);

            var match = (from t in r.Elements
                         where
                             t.IsOneOfThese(hitDetectionTypes) &&
                             hitDetectionExclusions.Contains(t) == false &&
                             Target != t &&
                             testArea.NumberOfPixelsThatOverlap(t) > 0
                         select t).OrderBy(t => t.Center().CalculateDistanceTo(Target.Center()));


            if (match.Count() == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
            }
            else
            {
                prediction.ElementHit = match.First();
                prediction.Type = HitType.Element;
                prediction.Direction = testArea.GetHitDirection(match.First().Bounds);
                prediction.BoundsOfItemBeingHit = prediction.ElementHit.Bounds;
            }

            return prediction;
        }
    }
}