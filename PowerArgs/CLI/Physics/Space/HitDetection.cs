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
        public IRectangularF MovingObjectPosition { get; set; }
        public HitType Type { get; set; }
        public Direction Direction { get; set; }
        public IRectangularF ObstacleHit { get; set; }
        public ILocationF LKG { get; set; }
        public float Visibility { get; set; }

        public List<IRectangularF> Path { get; set; } = new List<IRectangularF>();
    }

    public class HitDetectionOptions
    {
        public IRectangularF MovingObject { get; set; }
        public IEnumerable<IRectangularF> Obstacles { get; set; }
        public float Angle { get; set; }
        public float Visibility { get; set; } 
        public float Precision { get; set; } = .1f;
    }

    public static class HitDetection
    {
        public static bool HasLineOfSight(this Velocity from, IRectangularF to) => HasLineOfSight(from.Element, to, from.GetObstacles());
        public static bool HasLineOfSight(this SpacialElement from, IRectangularF to) => HasLineOfSight(from, to, from.GetObstacles());
        public static bool HasLineOfSight(this IRectangularF from, IRectangularF to, IEnumerable<IRectangularF> obstacles) => GetLineOfSightObstruction(from, to, obstacles) == null;

        public static IRectangularF GetLineOfSightObstruction(this IRectangularF from, IRectangularF to, IEnumerable<IRectangularF> obstacles)
        {
            var prediction = PredictHit(new HitDetectionOptions()
            {
                MovingObject = from,
                Angle = from.Center().CalculateAngleTo(to.Center()),
                Obstacles = obstacles.Union(new IRectangularF[] { to }),
                Visibility = 3 * from.Center().CalculateDistanceTo(to.Center()),
            });

            if (prediction.Type == HitType.None)
            {
                return SpaceTime.CurrentSpaceTime.Bounds;
            }
            else
            {
                return prediction.ObstacleHit == to ? null : prediction.ObstacleHit;
            }
        }

        public static HitPrediction PredictHit(HitDetectionOptions options)
        {
            HitPrediction prediction = new HitPrediction();
            prediction.MovingObjectPosition = options.MovingObject.CopyBounds();
            prediction.Visibility = options.Visibility;
            if (options.Visibility == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
                return prediction;
            }

  
            var effectiveObstacles =  options.Obstacles.Where(o => o.CalculateDistanceTo(options.MovingObject) <= options.Visibility+options.Precision).ToList();

            var endPoint = options.MovingObject.MoveTowards(options.Angle, options.Visibility);
            ILocationF lkg = null;
            for(var dPrime = options.Precision; dPrime < options.Visibility; dPrime+=options.Precision)
            {
                var testArea = options.MovingObject.MoveTowards(options.Angle, dPrime);
                prediction.Path.Add(testArea);
                var obstacleHit = effectiveObstacles.Where(o => o.Touches(testArea) == true).FirstOrDefault();

                if(obstacleHit != null)
                {

                    prediction.Type = HitType.Obstacle;
                    prediction.ObstacleHit = obstacleHit;
                    prediction.LKG = lkg;
                    return prediction;
                }
                else
                {
                    lkg = testArea.TopLeft();
                }
            }

            var obstacleHitFinal = effectiveObstacles.Where(o => o.Touches(endPoint) == true).FirstOrDefault();

            if (obstacleHitFinal != null)
            {
                prediction.Type = HitType.Obstacle;
                prediction.ObstacleHit = obstacleHitFinal;
                prediction.LKG = lkg;
                return prediction;
            }

            prediction.Type = HitType.None;
            return prediction;
        }
    }
}