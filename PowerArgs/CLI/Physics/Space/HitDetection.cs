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
        public float LKGD { get; set; }
        public float Visibility { get; set; }
        public bool ElementWasAlreadyObstructed { get; set; }

        public int EdgeIndex { get; set; }
    }

    public class HitDetectionOptions
    {
        public IRectangularF MovingObject { get; set; }
        public IEnumerable<IRectangularF> Obstacles { get; set; }
        public float Angle { get; set; }
        public float Visibility { get; set; } 

        public CastingMode Mode { get; set; } = CastingMode.Precise;
    }

    public enum CastingMode
    {
        SingleRay,
        Rough,
        Precise
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
                Mode = CastingMode.Rough,
            });

            if (prediction.Type == HitType.None)
            {
                return SpaceTime.CurrentSpaceTime?.Bounds;
            }
            else
            {
                if (to is IHaveMassBounds && prediction.ObstacleHit is SpacialElement && (to as IHaveMassBounds).IsPartOfMass(prediction.ObstacleHit as SpacialElement))
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


            var mov = options.MovingObject;

            List<Edge> rays;
            if (options.Mode == CastingMode.Precise)
            {
                rays = new List<Edge>()
                {
                    new Edge() { From = mov.TopLeft(), To = mov.TopLeft().MoveTowards(options.Angle, options.Visibility, normalized:false) },
                    new Edge() { From = mov.TopRight(), To = mov.TopRight().MoveTowards(options.Angle, options.Visibility, normalized:false) },
                    new Edge() { From = mov.BottomLeft(), To = mov.BottomLeft().MoveTowards(options.Angle, options.Visibility, normalized:false) },
                    new Edge() { From = mov.BottomRight(), To = mov.BottomRight().MoveTowards(options.Angle, options.Visibility, normalized:false) },
                };

                var granularity = .5f;

                for (var x = mov.Left + granularity; x < mov.Left + mov.Width; x += granularity)
                {
                    var top = LocationF.Create(x, mov.Top);
                    var bot = LocationF.Create(x, mov.Bottom());

                    rays.Add(new Edge() { From = top, To = top.MoveTowards(options.Angle, options.Visibility, normalized: false) });
                    rays.Add(new Edge() { From = bot, To = bot.MoveTowards(options.Angle, options.Visibility, normalized: false) });
                }

                for (var y = mov.Top + granularity; y < mov.Top + mov.Height; y += granularity)
                {
                    var left = LocationF.Create(mov.Left, y);
                    var right = LocationF.Create(mov.Right(), y);

                    rays.Add(new Edge() { From = left, To = left.MoveTowards(options.Angle, options.Visibility, normalized: false) });
                    rays.Add(new Edge() { From = right, To = right.MoveTowards(options.Angle, options.Visibility, normalized: false) });
                }
            }
            else if (options.Mode == CastingMode.Rough)
            {
                var center = options.MovingObject.Center();
                rays = new List<Edge>() 
                {
                    new Edge() { From = mov.TopLeft(), To = mov.TopLeft().MoveTowards(options.Angle, options.Visibility, normalized:false) },
                    new Edge() { From = mov.TopRight(), To = mov.TopRight().MoveTowards(options.Angle, options.Visibility, normalized:false) },
                    new Edge() { From = mov.BottomLeft(), To = mov.BottomLeft().MoveTowards(options.Angle, options.Visibility, normalized:false) },
                    new Edge() { From = mov.BottomRight(), To = mov.BottomRight().MoveTowards(options.Angle, options.Visibility, normalized:false) },
                    new Edge() { From = center, To = center.MoveTowards(options.Angle, options.Visibility, normalized: false) }
                };
            }
            else if(options.Mode == CastingMode.SingleRay)
            {
                var center = options.MovingObject.Center();
                rays = new List<Edge>()
                {
                    new Edge() { From = center, To = center.MoveTowards(options.Angle, options.Visibility, normalized: false) }
                };
            }
            else
            {
                throw new NotSupportedException("Unknown mide: "+options.Mode);
            }

            var closestIntersectionDistance = float.MaxValue;
            IRectangularF closestIntersectingElement = null;
            var closestEdgeIndex = -1;
            var effectiveObstacles = options.Obstacles.ToArray();
            for (var i = 0; i < effectiveObstacles.Length; i++)
            {
                var obstacle = effectiveObstacles[i];
                for(var j = 0; j < obstacle.Edges.Length; j++)
                {
                    var edge = obstacle.Edges[j];
                    for(var k = 0; k < rays.Count; k++)
                    {
                        var ray = rays[k];
                        var intersection = FindIntersectionPoint(ray, edge);
                        if (intersection != null)
                        {
                            var d = ray.From.CalculateDistanceTo(intersection);
                            if (d < closestIntersectionDistance && d <= options.Visibility)
                            {
                                closestIntersectionDistance = d;
                                closestIntersectingElement = obstacle;
                                closestEdgeIndex = j;
                            }
                        }
                    }
                }
            }

            if(closestIntersectingElement != null)
            {
                prediction.ObstacleHit = closestIntersectingElement;
                prediction.LKGD = closestIntersectionDistance - .1f;
                prediction.LKG = options.MovingObject.MoveTowards(options.Angle, prediction.LKGD, normalized:false).TopLeft();
                prediction.Type = HitType.Obstacle;
                prediction.EdgeIndex = closestEdgeIndex;
            }

            return prediction;
        }

        private static ILocationF FindIntersectionPoint(Edge a, Edge b)
        {
            var x1 = a.From.Left;
            var y1 = a.From.Top;
            var x2 = a.To.Left;
            var y2 = a.To.Top;

            var x3 = b.From.Left;
            var y3 = b.From.Top;
            var x4 = b.To.Left;
            var y4 = b.To.Top;

            var den = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (den == 0)
            {
                return null;
            }

            var t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / den;
            if(t <= 0 || t >= 1)
            {
                return null;
            }

            var u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / den;
            if (u > 0 && u < 1)
            {
                return  LocationF.Create(x1 + t * (x2 - x1), y1 + t * (y2 - y1));
            }
            else
            {
                return null;
            }
        }
    }
}