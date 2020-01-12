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
        public float Angle { get; set; }
        public float Visibility { get; set; }
        public float Precision { get; set; } = .1f;
    }

    public static class HitDetection
    {
        public static HitPrediction PredictHit(HitDetectionOptions options)
        {
            HitPrediction prediction = new HitPrediction();
            if (options.Visibility == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
                return prediction;
            }

  
            var effectiveObstacles =  options.Obstacles.Where(o => Geometry.CalculateNormalizedDistanceTo(o,options.MovingObject) <= options.Visibility).ToList();

            var endPoint = options.MovingObject.MoveTowards(options.Angle, options.Visibility);
            ILocationF lkg = null;
            for(var dPrime = options.Precision; dPrime < options.Visibility; dPrime+=options.Precision)
            {
                var testArea = options.MovingObject.MoveTowards(options.Angle, dPrime);
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