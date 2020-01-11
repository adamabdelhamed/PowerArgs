using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public enum HitType
    {
        None = 0,
        Obstacle = 1,
    }

    public class Impact
    {
        public float Angle { get; set; }
        public SpacialElement MovingObject { get; set; }
        public IRectangularF ObstacleHit { get; set; }
        public HitType HitType { get; set; }
    }

    public class HitPrediction
    {
        public HitType Type { get; set; }
        public Direction Direction { get; set; }
        public IRectangularF ObstacleHit { get; set; }
        public ILocationF LKG { get; set; }
    }

    public class HitDetectionOptions
    {
        public IRectangularF Bounds { get; set; }
        public IRectangularF MovingObject { get; set; }
        public IEnumerable<IRectangularF> Obstacles { get; set; }
        public float Dx { get; set; }
        public float Dy { get; set; }
        public float Precision { get; set; } = .1f;
    }

    public class SonarOptions
    {
        public IRectangularF Bounds { get; set; }
        public IRectangularF MovingObject { get; set; }
        public IEnumerable<IRectangularF> Obstacles { get; set; }
        public float Angle { get; set; }
        public float Visibility { get; set; } = Geometry.Hypotenous(SpaceTime.CurrentSpaceTime.Bounds);
        public float Precision { get; set; } = .1f;
    }

    public static class HitDetection
    {
        public static HitPrediction Sonar(SonarOptions options)
        {
            var innerOptions = new HitDetectionOptions()
            {
                Bounds = options.Bounds,
                MovingObject = options.MovingObject,
                Precision = options.Precision,
                Obstacles = options.Obstacles,
            };
            for (var d = options.Precision; d < options.Visibility; d+=options.Precision)
            {
                var loc = options.MovingObject.TopLeft().MoveTowards(options.Angle, d);
                innerOptions.Dx = loc.Left - options.MovingObject.Left;
                innerOptions.Dy = loc.Top - options.MovingObject.Top;
                var prediction = PredictHit(innerOptions);
                if(prediction.Type != HitType.None)
                {
                    return prediction;
                }
            }

            return new HitPrediction()
            {
                Type = HitType.None,
            };
        }

        public static HitPrediction PredictHit(HitDetectionOptions options)
        {
            HitPrediction prediction = new HitPrediction();

            var left = Math.Min(options.MovingObject.Left, options.MovingObject.Left + options.Dx);
            var top = Math.Min(options.MovingObject.Top, options.MovingObject.Top + options.Dy);
            var right = Math.Max(options.MovingObject.Left + options.MovingObject.Width, options.MovingObject.Left + options.MovingObject.Width + options.Dx);
            var bottom = Math.Max(options.MovingObject.Top + options.MovingObject.Height, options.MovingObject.Top + options.MovingObject.Height + options.Dy);
            var relevantArea = RectangularF.Create(left, top, right - left, bottom - top);

            var effectiveObstacles =  options.Obstacles.Where(o => o.Touches(relevantArea)).ToList();

            if (options.Dx == 0 && options.Dy == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
                return prediction;
            }

            var endPoint = RectangularF.Create(options.MovingObject.Left + options.Dx, options.MovingObject.Top + options.Dy, options.MovingObject.Width, options.MovingObject.Height);
            var angle = options.MovingObject.CalculateAngleTo(endPoint);
            var d = endPoint.Center().CalculateDistanceTo(options.MovingObject.Center());
            ILocationF lkg = null;
            for(var dPrime = options.Precision; dPrime < d; dPrime+=options.Precision)
            {
                var testLocation = options.MovingObject.Center().MoveTowards(angle, dPrime);
                var testArea = RectangularF.Create(testLocation.Left - options.MovingObject.Width / 2, testLocation.Top - options.MovingObject.Height / 2, options.MovingObject.Width, options.MovingObject.Height);
                var obstacleHit = effectiveObstacles.Where(o => o.Touches(testArea) == true).FirstOrDefault();

                if(obstacleHit != null)
                {
                    return new HitPrediction()
                    {
                        Type = HitType.Obstacle,
                        ObstacleHit = obstacleHit,
                        LKG = lkg,
                    };
                }
                else
                {
                    lkg = testArea.TopLeft();
                }
            }

            var obstacleHitFinal = effectiveObstacles.Where(o => o.Touches(endPoint) == true).FirstOrDefault();

            if (obstacleHitFinal != null)
            {
                return new HitPrediction()
                {
                    Type = HitType.Obstacle,
                    ObstacleHit = obstacleHitFinal,
                    LKG = lkg
                };
            }

            prediction.Type = HitType.None;
            return prediction;
        }
    }
}