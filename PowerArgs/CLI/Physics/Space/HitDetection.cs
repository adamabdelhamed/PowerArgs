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
    }

    public class HitDetectionOptions
    {
        public IRectangularF Bounds { get; set; }
        public IRectangularF MovingObject { get; set; }
        public List<IRectangularF> Obstacles { get; set; }
        public List<IRectangularF> Exclusions { get; set; }
        public float Dx { get; set; }
        public float Dy { get; set; }
        public float Precision { get; set; } = .1f;
    }

    public static class HitDetection
    {

        public static HitPrediction PredictHit(HitDetectionOptions options)
        {
            HitPrediction prediction = new HitPrediction();

            if (options.Dx == 0 && options.Dy == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
                return prediction;
            }

            var endPoint = RectangularF.Create(options.MovingObject.Left + options.Dx, options.MovingObject.Top + options.Dy, options.MovingObject.Width, options.MovingObject.Height);
            var angle = options.MovingObject.CalculateAngleTo(endPoint);
            var d = endPoint.CalculateDistanceTo(options.MovingObject);
            for(var dPrime = options.Precision; dPrime < d; dPrime+=options.Precision)
            {
                var testLocation = options.MovingObject.Center().MoveTowards(angle, dPrime);
                var testArea = RectangularF.Create(testLocation.Left - options.MovingObject.Width / 2, testLocation.Top - options.MovingObject.Height / 2, options.MovingObject.Width, options.MovingObject.Height);
                var obstacleHit = options.Obstacles.Where(o => IsIncluded(options,o) && o.Touches(testArea) == true).FirstOrDefault();

                if(obstacleHit != null)
                {
                    return new HitPrediction()
                    {
                        Type = HitType.Obstacle,
                        ObstacleHit = obstacleHit
                    };
                }
            }

            var obstacleHitFinal = options.Obstacles.Where(o => IsIncluded(options, o) && o.Touches(endPoint) == true).FirstOrDefault();

            if (obstacleHitFinal != null)
            {
                return new HitPrediction()
                {
                    Type = HitType.Obstacle,
                    ObstacleHit = obstacleHitFinal
                };
            }

            prediction.Type = HitType.None;
            return prediction;
        }

        private static bool IsIncluded(HitDetectionOptions options, IRectangularF obj)
        {
            if (options.Exclusions == null) return true;
            else return options.Exclusions.Contains(obj) == false;
        }
    }
}