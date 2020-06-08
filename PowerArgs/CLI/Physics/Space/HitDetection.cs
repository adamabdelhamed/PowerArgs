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
        public bool ElementWasAlreadyObstructed { get; set; }

        public List<IRectangularF> Path { get; set; } = new List<IRectangularF>();
    }

    public class HitDetectionOptions
    {
        public IRectangularF MovingObject { get; set; }
        public IEnumerable<IRectangularF> Obstacles { get; set; }
        public float Angle { get; set; }
        public float Visibility { get; set; } 
        public float Precision { get; set; } = .2f;
    }

    public static class HitDetection
    {
        public static bool HasLineOfSight(this Velocity from, IRectangularF to, float? precision = null) => HasLineOfSight(from.Element, to, from.GetObstacles(), precision);
        public static bool HasLineOfSight(this SpacialElement from, IRectangularF to, float? precision = null) => HasLineOfSight(from, to, from.GetObstacles(), precision);
        public static bool HasLineOfSight(this IRectangularF from, IRectangularF to, IEnumerable<IRectangularF> obstacles, float? precision = null) => GetLineOfSightObstruction(from, to, obstacles, precision) == null;

        public static IRectangularF GetLineOfSightObstruction(this IRectangularF from, IRectangularF to, IEnumerable<IRectangularF> obstacles, float? precision = null)
        {
            var effectivePrecision = precision.HasValue ? precision.Value : .2f;
            var prediction = PredictHit(new HitDetectionOptions()
            {
                MovingObject = from,
                Angle = from.Center().CalculateAngleTo(to.Center()),
                Obstacles = obstacles.Union(new IRectangularF[] { to }),
                Visibility = 3 * from.Center().CalculateDistanceTo(to.Center()),
                Precision = effectivePrecision,
            });

            if (prediction.Type == HitType.None)
            {
                return SpaceTime.CurrentSpaceTime?.Bounds;
            }
            else
            {
                if(to is IHaveMassBounds && prediction.ObstacleHit is SpacialElement && (to as IHaveMassBounds).IsPartOfMass(prediction.ObstacleHit as SpacialElement))
                {
                    return null;
                }

                if (prediction.ObstacleHit is SpacialElement && (to is IHaveMassBounds) && (to as IHaveMassBounds).IsPartOfMass((SpacialElement)prediction.ObstacleHit))
                {
                    return null;
                }
                else
                {
                    return prediction.ObstacleHit == to ? null : prediction.ObstacleHit;
                }
            }
        }

        public static HitPrediction PredictHit(HitDetectionOptions options)
        {
            HitPrediction prediction = new HitPrediction();
            prediction.LKG = options.MovingObject.CopyBounds().TopLeft();
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
            for(var dPrime = options.Precision; dPrime < options.Visibility; dPrime+=options.Precision)
            {
                var testArea = options.MovingObject.MoveTowards(options.Angle, dPrime);
                prediction.Path.Add(testArea);
                var obstacleHit = effectiveObstacles.Where(o =>
                {
                    var simpleTest = o.Touches(testArea) == true;
                    if (simpleTest == false) return false;

                    if(o.Touches(options.MovingObject))
                    {
                        prediction.ElementWasAlreadyObstructed = true;
                        var overlapBefore = options.MovingObject.NumberOfPixelsThatOverlap(o);
                        var overlapAfter = testArea.NumberOfPixelsThatOverlap(o);

                        IRectangularF testArea2 = null;
                        while(overlapBefore == overlapAfter)
                        {
                            testArea2 = testArea2 ?? testArea.CopyBounds();
                            testArea2 = testArea2.MoveTowards(options.Angle, options.Precision);
                            overlapAfter = testArea2.NumberOfPixelsThatOverlap(o);
                        }

                        return overlapAfter > overlapBefore;
                    }
                    else
                    {
                        return true;
                    }
                }).FirstOrDefault();

                if(obstacleHit != null)
                {

                    prediction.Type = HitType.Obstacle;
                    prediction.ObstacleHit = obstacleHit;
                    return prediction;
                }
                else
                {
                    prediction.LKG = testArea.TopLeft();
                }
            }

            var obstacleHitFinal = effectiveObstacles.Where(o => o.Touches(endPoint) == true).FirstOrDefault();

            if (obstacleHitFinal != null)
            {
                prediction.Type = HitType.Obstacle;
                prediction.ObstacleHit = obstacleHitFinal;
                return prediction;
            }

            prediction.Type = HitType.None;
            return prediction;
        }
    }
}