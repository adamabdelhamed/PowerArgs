using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public enum HitType
    {
        None = 0,
        Obstacle = 1,
        Boundary = 2,
    }

    public class Impact
    {
        public float Angle { get; set; }
        public SpacialElement MovingObject { get; set; }
        public IRectangular ObstacleHit { get; set; }
        public HitType HitType { get; set; }
    }

    public class HitPrediction
    {
        public HitType Type { get; set; }
        public Direction Direction { get; set; }
        public IRectangular ObstacleHit { get; set; }
    }

    public class HitDetectionOptions
    {
        public IRectangular Bounds { get; set; }
        public IRectangular MovingObject { get; set; }
        public List<IRectangular> Obstacles { get; set; }
        public List<IRectangular> Exclusions { get; set; }
        public float Dx { get; set; }
        public float Dy { get; set; }
        public int Precision { get; set; } = 5;

        internal HitDetectionOptions ShallowCopy()
        {
            return new HitDetectionOptions()
            {
                Bounds = this.Bounds,
                MovingObject = this.MovingObject,
                Obstacles = this.Obstacles,
                Dx = this.Dx,
                Dy = this.Dy,
                Precision = this.Precision,
            };
        }
    }

    public static class HitDetection
    {

        public static HitPrediction PredictHit(HitDetectionOptions options)
        {
            if (Math.Abs(options.Dx) <= options.Precision && Math.Abs(options.Dy) <= options.Precision)
            {
                return PredictHitInternal(options);
            }

            HitPrediction latestResult = null;
            for (var i = 1; i <= options.Precision; i++)
            {
                var dxP = Approach(0, options.Dx, options.Dx / options.Precision * i);
                var dyP = Approach(0, options.Dy, options.Dy / options.Precision * i);
                var approachingOptions = options.ShallowCopy();
                approachingOptions.Dx = dxP;
                approachingOptions.Dy = dyP;
                latestResult = PredictHitInternal(approachingOptions);
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

        private static HitPrediction PredictHitInternal(HitDetectionOptions options)
        {
            HitPrediction prediction = new HitPrediction();

            if(options.Dx == 0 && options.Dy == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
                return prediction;
            }

            if (options.Dy > 0 && options.MovingObject.Bottom() + options.Dy >= options.Bounds.Height)
            {
                prediction.Direction = Direction.Down;
                prediction.Type = HitType.Boundary;
                prediction.ObstacleHit = Rectangular.Create(options.MovingObject.Left + options.Dx, options.Bounds.Height + options.Dy, 1, 1);
                return prediction;
            }
            else if (options.Dx < 0 && options.MovingObject.Left + options.Dx <= 0)
            {
                prediction.Direction = Direction.Left;
                prediction.Type = HitType.Boundary;
                prediction.ObstacleHit = Rectangular.Create(-options.Dx, options.MovingObject.Top + options.Dy, 1, 1);
                return prediction;
            }
            else if (options.Dy < 0 && options.MovingObject.Top + options.Dy <= 0)
            {
                prediction.Direction = Direction.Up;
                prediction.Type = HitType.Boundary;
                prediction.ObstacleHit = Rectangular.Create(options.MovingObject.Left + options.Dx, -options.Dy, 1, 1);
                return prediction;
            }
            else if (options.Dx > 0 && options.MovingObject.Right() + options.Dx >= options.Bounds.Width)
            {
                prediction.Direction = Direction.Right;
                prediction.Type = HitType.Boundary;
                prediction.ObstacleHit = Rectangular.Create(options.Bounds.Width + options.Dx, options.MovingObject.Top + options.Dy, 1, 1);
                return prediction;
            }

            var testArea = Rectangular.Create(options.MovingObject.Left + options.Dx, options.MovingObject.Top + options.Dy, options.MovingObject.Width, options.MovingObject.Height);


            var obstacleToBeHit = options.Obstacles
                .Where(o => o.Touches(testArea) && o.Touches(options.MovingObject) == false && options.Exclusions.Contains(o) == false).FirstOrDefault();

            if (obstacleToBeHit == null)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
            }
            else
            {
                prediction.Type = HitType.Obstacle;
                prediction.Direction = testArea.GetHitDirection(obstacleToBeHit);
                prediction.ObstacleHit = obstacleToBeHit;
            }

            return prediction;
        }
    }
}